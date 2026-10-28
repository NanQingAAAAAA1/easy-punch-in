using System.Drawing;
using System.Windows.Forms;

namespace EasyPunchIn
{
    /// <summary>
    /// 打卡 / 关机确认弹窗。外观（标题、正文、按钮文字、颜色、字体、置顶）全部来自 Config。
    /// 点击按钮触发 Confirmed 事件，由调用方决定是记录打卡还是放行关机。
    /// </summary>
    public class ReminderForm : Form
    {
        public event Action? Confirmed;
        private readonly Config _config;

        public ReminderForm(Config config, string title, string body)
        {
            _config = config;
            BuildUi(title, body);
        }

        private void BuildUi(string title, string body)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            TopMost = _config.TopMost;
            Width = 440;
            Height = 240;

            var bg = ParseColor(_config.BackgroundColor, Color.FromArgb(0x2D, 0x2D, 0x30));
            var fg = ParseColor(_config.TextColor, Color.White);
            var accent = ParseColor(_config.AccentColor, Color.FromArgb(0x00, 0x7A, 0xCC));

            BackColor = bg;
            ForeColor = fg;

            var label = new Label
            {
                Text = body,
                ForeColor = fg,
                Font = new Font(_config.FontName, _config.FontSize),
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(label);

            var bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 64,
                BackColor = bg,
                Padding = new Padding(16, 12, 16, 12)
            };

            var button = new Button
            {
                Text = _config.ButtonText,
                BackColor = accent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(_config.FontName, _config.FontSize, FontStyle.Bold),
                Dock = DockStyle.Right,
                Width = 180
            };
            button.FlatAppearance.BorderSize = 0;
            button.Click += (_, _) => Confirmed?.Invoke();

            bottom.Controls.Add(button);
            Controls.Add(bottom);

            if (!string.IsNullOrWhiteSpace(_config.IconPath) && File.Exists(_config.IconPath))
            {
                try { Icon = new Icon(_config.IconPath); } catch { }
            }
        }

        private static Color ParseColor(string? hex, Color fallback)
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            try { return ColorTranslator.FromHtml(hex); }
            catch { return fallback; }
        }
    }
}
