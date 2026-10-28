using System.Text.Json;

namespace EasyPunchIn
{
    /// <summary>
    /// JSONL 日志：每行一条事件记录，便于排查与统计未确认次数。
    /// 日志文件位于 %AppData%\PunchPal\log.jsonl
    /// </summary>
    public class Logger
    {
        private static string LogDir =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PunchPal");

        private static string LogPath => Path.Combine(LogDir, "log.jsonl");

        public void Write(string eventName, string action, Dictionary<string, object?>? extra = null)
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                var entry = new Dictionary<string, object?>
                {
                    ["timestamp"] = DateTime.Now.ToString("o"),
                    ["event"] = eventName,
                    ["action"] = action
                };
                if (extra != null)
                    foreach (var kv in extra)
                        entry[kv.Key] = kv.Value;

                File.AppendAllText(LogPath, JsonSerializer.Serialize(entry) + Environment.NewLine);
            }
            catch
            {
                // 日志写入失败不影响主流程
            }
        }
    }
}
