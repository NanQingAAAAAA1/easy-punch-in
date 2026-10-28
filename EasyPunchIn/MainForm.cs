using System.Diagnostics;
using System.Media;
using System.Windows.Forms;

namespace EasyPunchIn
{
    /// <summary>
    /// 主窗体（默认最小化到托盘，不出现在任务栏）。
    /// 负责：
    ///  - 开机延迟后检查上班打卡提醒
    ///  - 到下班时间弹出下班打卡提醒
    ///  - 拦截 Windows 关机/注销/重启（WM_QUERYENDSESSION + ShutdownBlockReasonCreate）
    ///  - 托盘菜单与配置打开
    /// </summary>
    public class MainForm : Form
    {
        private const int WM_QUERYENDSESSION = 0x0011;
        private const int WM_ENDSESSION = 0x0016;

        // WM_QUERYENDSESSION / WM_ENDSESSION 的 lParam 位掩码（wParam 保留未用，恒为 0）
        private const long ENDSESSION_CLOSEAPP = 0x00000001L;
        private const long ENDSESSION_CRITICAL = 0x40000000L;
        private const long ENDSESSION_LOGOFF = 0x80000000L;

        private readonly Config _config;
        private readonly Logger _logger;
        private readonly State _state;
        private readonly NotifyIcon _tray;

        private bool _allowShutdown;
        private bool _shutdownHandled;
        private readonly bool _pendingMissedShutdown;
        private ReminderForm? _activeReminder;

        // 本轮会话（本次开机）是否已弹过对应提醒。
        // 关键：只做"会话内去重"，不再跨天去重 —— 这样一天内多次开关机，每轮都会重新弹窗。
        private bool _clockInShownThisSession;
        private bool _clockOutShownThisSession;

        public MainForm(Config config, Logger logger, State state)
        {
            _config = config;
            _logger = logger;
            _state = state;

            // 启动即记一条日志，便于事后判断“开机自启到底有没有跑起来”
            _logger.Write("app", "started",
                new Dictionary<string, object?> { ["autoStart"] = AutoStartHelper.IsAutoStartEnabled() });

            Text = "打卡小护卫 PunchPal";
            WindowState = FormWindowState.Minimized;
            ShowInTaskbar = false;
            Load += (_, _) =>
            {
                ShowInTaskbar = false;
                if (_pendingMissedShutdown)
                    ShowReminder("missed_shutdown", "上次关机提醒",
                        "上次电脑关机前您未完成“下班打卡”确认。\n请记得今天及时打卡。");
            };

            _tray = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "打卡小护卫 PunchPal",
                Visible = true,
                ContextMenuStrip = BuildTrayMenu()
            };
            _tray.DoubleClick += (_, _) => OpenConfig();

            // 上次关机未确认 -> 本次开机补提醒。
            // 注意：此处仍在构造函数中，窗口句柄尚未创建，不能调用 BeginInvoke/Show，
            // 因此先记住标记并复位，实际弹窗推迟到 Load 事件（句柄已就绪）中执行。
            _pendingMissedShutdown = _state.PendingUnconfirmedShutdown;
            if (_pendingMissedShutdown)
            {
                _state.PendingUnconfirmedShutdown = false;
                _state.Save();
            }

            // 上班打卡提醒：启动延迟后弹出（每轮会话都弹，支持一天内多次开关机）
            var clockInTimer = new System.Windows.Forms.Timer
            {
                Interval = Math.Max(1000, _config.ClockInReminderDelaySeconds * 1000)
            };
            clockInTimer.Tick += (_, _) =>
            {
                clockInTimer.Stop();
                if (!_clockInShownThisSession)
                {
                    _clockInShownThisSession = true;
                    ShowReminder("clockin", _config.Title, BodyOr(_config.ClockInBody), MarkClockIn);
                }
            };
            clockInTimer.Start();

            // 下班打卡提醒：每 30 秒轮询，到设定时间后弹一次（每轮会话都弹，支持一天内多次开关机）
            var clockOutTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            clockOutTimer.Tick += (_, _) =>
            {
                if (!TimeSpan.TryParse(_config.ClockOutTime, out var t)) return;
                if (DateTime.Now.TimeOfDay >= t && !_clockOutShownThisSession)
                {
                    _clockOutShownThisSession = true;
                    ShowReminder("clockout", _config.ClockOutTitle, BodyOr(_config.ClockOutBody), MarkClockOut);
                }
            };
            clockOutTimer.Start();
        }

        private ContextMenuStrip BuildTrayMenu()
        {
            var menu = new ContextMenuStrip();

            var confirmIn = new ToolStripMenuItem("确认上班打卡");
            confirmIn.Click += (_, _) => MarkClockIn();

            var confirmOut = new ToolStripMenuItem("确认下班打卡");
            confirmOut.Click += (_, _) => MarkClockOut();

            var show = new ToolStripMenuItem("显示上班提醒");
            show.Click += (_, _) => ShowReminder("clockin", _config.Title, BodyOr(_config.ClockInBody), MarkClockIn);

            var cfg = new ToolStripMenuItem("打开配置");
            cfg.Click += (_, _) => OpenConfig();

            var auto = new ToolStripMenuItem("开机自启: " + (AutoStartHelper.IsAutoStartEnabled() ? "开" : "关"));
            auto.Click += (_, _) =>
            {
                if (AutoStartHelper.IsAutoStartEnabled())
                    AutoStartHelper.RemoveAutoStart();
                else
                    AutoStartHelper.EnsureAutoStart();
                auto.Text = "开机自启: " + (AutoStartHelper.IsAutoStartEnabled() ? "开" : "关");
            };

            var exit = new ToolStripMenuItem("退出");
            exit.Click += (_, _) =>
            {
                _allowShutdown = true;
                _tray.Visible = false;
                Application.Exit();
            };

            menu.Items.AddRange(new ToolStripItem[] { confirmIn, confirmOut, show, cfg, auto, exit });
            return menu;
        }

        private static string DateToday() => DateTime.Today.ToString("yyyy-MM-dd");

        // 优先用细分文案（ClockInBody / ClockOutBody），为空则回退到通用 Body
        private string BodyOr(string specific) => string.IsNullOrWhiteSpace(specific) ? _config.Body : specific;

        // 今天是否已确认下班打卡
        private bool IsClockOutConfirmedToday() => _state.LastClockOutConfirmDate == DateToday();

        private void MarkClockIn()
        {
            _state.LastClockInConfirmDate = DateToday();
            _state.Save();
            _logger.Write("clockin", "confirmed");
        }

        private void MarkClockOut()
        {
            _state.LastClockOutConfirmDate = DateToday();
            _state.Save();
            _logger.Write("clockout", "confirmed");
        }

        private void ShowReminder(string kind, string title, string body, Action? onConfirm = null)
        {
            if (_activeReminder != null)
            {
                if (kind == "shutdown")
                {
                    // 关机确认优先级最高：关掉其它提醒，确保关机确认框一定出现
                    _activeReminder.Close();
                }
                else
                {
                    _activeReminder.Activate();
                    return;
                }
            }
            _logger.Write(kind, "shown");
            var form = new ReminderForm(_config, title, body);
            form.Confirmed += () =>
            {
                onConfirm?.Invoke();
                form.Close();
            };
            form.FormClosed += (_, _) => _activeReminder = null;
            _activeReminder = form;
            form.Show();
            PlaySound();
        }

        private void AllowShutdown()
        {
            _allowShutdown = true;
            ShutdownHelper.DestroyReason(Handle);
            _logger.Write("shutdown", "confirmed");
            // 销毁阻止原因后，若系统挂起的关机流程未自动继续，用户再次点击关机即放行；
            // 放行标记会在收到 WM_ENDSESSION 后复位，保证“每次关机都要确认”。
        }

        private void OpenConfig()
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = Config.ConfigPath, UseShellExecute = true });
            }
            catch
            {
                // 配置文件可能尚未生成
            }
        }

        private void PlaySound()
        {
            if (!string.IsNullOrWhiteSpace(_config.SoundPath) && File.Exists(_config.SoundPath))
            {
                try { new SoundPlayer(_config.SoundPath).Play(); } catch { }
            }
        }

        protected override void WndProc(ref Message m)
        {
            // 关机 / 注销 / 重启前拦截
            if (m.Msg == WM_QUERYENDSESSION)
            {
                // 重要：WM_QUERYENDSESSION 的 wParam 保留未用（恒为 0），
                // 关机原因在 lParam 位掩码里。之前误用 wParam 判断，导致真实关机时从不拦截。
                long flags = m.LParam.ToInt64();
                bool critical = (flags & ENDSESSION_CRITICAL) != 0; // 强制关机，无法拦截
                bool mustBlock = _config.LockOnEveryShutdown || !IsClockOutConfirmedToday();

                if (!critical && !_allowShutdown && mustBlock)
                {
                    m.Result = IntPtr.Zero; // FALSE -> 拒绝关机
                    if (!_shutdownHandled)
                    {
                        _shutdownHandled = true;
                        ShutdownHelper.CreateReason(Handle, _config.ShutdownConfirmText);
                        _logger.Write("shutdown", "blocked",
                            new Dictionary<string, object?> { ["flags"] = flags });
                        BeginInvoke(() => ShowReminder("shutdown", "关机前确认", _config.ShutdownConfirmText, AllowShutdown));
                    }
                    return;
                }

                // 已确认 / 强制关机 / 无需拦截 -> 明确放行
                m.Result = new IntPtr(1); // TRUE -> 允许关机
                base.WndProc(ref m);
                return;
            }

            // 关机流程结束通知
            if (m.Msg == WM_ENDSESSION)
            {
                bool ending = m.WParam != IntPtr.Zero;
                if (ending)
                {
                    // 真正要关机了
                    if (!_allowShutdown)
                    {
                        // 被强制关机且未确认 -> 记录，下次开机补提醒
                        _state.PendingUnconfirmedShutdown = true;
                        _state.UnconfirmedCount++;
                        _state.Save();
                        _logger.Write("shutdown", "force_unconfirmed",
                            new Dictionary<string, object?> { ["unconfirmedCount"] = _state.UnconfirmedCount });
                    }
                    else
                    {
                        _logger.Write("shutdown", "proceeded");
                    }
                }
                else
                {
                    // 关机被取消
                    ShutdownHelper.DestroyReason(Handle);
                    _shutdownHandled = false;
                }

                // 关键：无论关机成功与否，都要复位放行标记。
                // 因为设置了“每次关机都确认”，若不复位，本次确认会一直生效到下次重启。
                _allowShutdown = false;

                base.WndProc(ref m);
                return;
            }

            base.WndProc(ref m);
        }
    }
}
