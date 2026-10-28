using System.Text.Json;

namespace EasyPunchIn
{
    /// <summary>
    /// 配置模型与读写。配置文件位于 %AppData%\PunchPal\config.json
    /// </summary>
    public class Config
    {
        public string Title { get; set; } = "打卡提醒";
        public string Body { get; set; } = "请确认您已完成打卡。";

        // 上班 / 下班提醒的正文（未单独配置时回退到 Body）
        public string ClockInBody { get; set; } = "请先确认上班打卡";
        public string ClockOutBody { get; set; } = "你下班打卡并带走水杯了吗？";

        // 下班打卡提醒的弹窗标题
        public string ClockOutTitle { get; set; } = "下班啦";

        public string ButtonText { get; set; } = "我已完成";

        public string BackgroundColor { get; set; } = "#2D2D30";
        public string TextColor { get; set; } = "#FFFFFF";
        public string AccentColor { get; set; } = "#4A4A4A";

        // 关机前确认时，是否强制每次都弹框确认（true = 每次关机都拦）
        public bool LockOnEveryShutdown { get; set; } = true;

        public string FontName { get; set; } = "Microsoft YaHei";
        public float FontSize { get; set; } = 14f;

        public string IconPath { get; set; } = "";
        public string SoundPath { get; set; } = "";
        public bool TopMost { get; set; } = true;

        public int ClockInReminderDelaySeconds { get; set; } = 15;
        public string ClockOutTime { get; set; } = "18:00";
        public string ShutdownConfirmText { get; set; } = "请先确认下班打卡与带走水杯后再关机";

        public bool AutoStart { get; set; } = true;

        public static string ConfigDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PunchPal");

        public static string ConfigPath => Path.Combine(ConfigDir, "config.json");

        public static Config Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    var cfg = JsonSerializer.Deserialize<Config>(json);
                    if (cfg != null) return cfg;
                }
            }
            catch
            {
                // 读取失败则回退到默认配置
            }

            var def = new Config();
            def.Save();
            return def;
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(ConfigDir);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch
            {
                // 保存失败静默处理
            }
        }
    }
}
