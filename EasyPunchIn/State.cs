using System.Text.Json;

namespace EasyPunchIn
{
    /// <summary>
    /// 跨会话状态：记录每日打卡确认日期、上次关机未确认标记与未确认累计次数。
    /// 位于 %AppData%\PunchPal\state.json
    /// </summary>
    public class State
    {
        public string? LastClockInConfirmDate { get; set; }
        public string? LastClockOutConfirmDate { get; set; }
        public bool PendingUnconfirmedShutdown { get; set; }
        public int UnconfirmedCount { get; set; }

        private static string Dir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PunchPal");

        public static string FilePath => Path.Combine(Dir, "state.json");

        public static State Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    var json = File.ReadAllText(FilePath);
                    var s = JsonSerializer.Deserialize<State>(json);
                    if (s != null) return s;
                }
            }
            catch
            {
                // 忽略损坏的状态文件
            }

            return new State();
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FilePath, json);
            }
            catch
            {
                // 状态保存失败静默处理
            }
        }
    }
}
