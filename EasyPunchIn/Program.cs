using System.Threading;
using System.Windows.Forms;
using EasyPunchIn;

namespace EasyPunchIn
{
    /// <summary>
    /// 程序入口：单实例运行、开机自启注册、托盘与关机拦截主窗体初始化。
    /// </summary>
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // 单实例：命名 Mutex 保证同一用户只运行一个实例
            using var mutex = new Mutex(true, "PunchPal-SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show("打卡小护卫已经在运行了。", "打卡小护卫 - 已在运行", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var config = Config.Load();
            // 按配置写入注册表开机自启（HKCU，无需管理员权限）
            if (config.AutoStart)
                AutoStartHelper.EnsureAutoStart();

            var logger = new Logger();
            var state = State.Load();

            Application.Run(new MainForm(config, logger, state));
        }
    }
}
