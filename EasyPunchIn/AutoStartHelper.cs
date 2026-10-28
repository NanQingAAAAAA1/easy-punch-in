using Microsoft.Win32;

namespace EasyPunchIn
{
    /// <summary>
    /// 注册表开机自启（当前用户 HKCU，无需管理员权限）。
    /// </summary>
    internal static class AutoStartHelper
    {
        private const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "PunchPal";

        public static void EnsureAutoStart()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true);
                if (key == null) return;
                // 路径加引号，兼容含空格/特殊字符的安装目录
                key.SetValue(ValueName, "\"" + Application.ExecutablePath + "\"");
            }
            catch
            {
                // 注册表写入失败静默处理
            }
        }

        public static void RemoveAutoStart()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(KeyPath, true);
                key?.DeleteValue(ValueName, false);
            }
            catch
            {
                // 忽略
            }
        }

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(KeyPath, false);
                return key?.GetValue(ValueName) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
