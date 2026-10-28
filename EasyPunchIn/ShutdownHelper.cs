using System.Runtime.InteropServices;

namespace EasyPunchIn
{
    /// <summary>
    /// 封装关机阻止原因 API：ShutdownBlockReasonCreate / ShutdownBlockReasonDestroy。
    /// 用于在关机流程中被拦截时，向系统提供“为什么不能关机”的可读原因。
    /// </summary>
    internal static class ShutdownHelper
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool ShutdownBlockReasonCreate(IntPtr hWnd, string reason);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ShutdownBlockReasonDestroy(IntPtr hWnd);

        public static void CreateReason(IntPtr handle, string reason)
        {
            try
            {
                if (handle != IntPtr.Zero)
                    ShutdownBlockReasonCreate(handle, reason);
            }
            catch
            {
                // API 不可用（如非交互会话）时静默跳过
            }
        }

        public static void DestroyReason(IntPtr handle)
        {
            try
            {
                if (handle != IntPtr.Zero)
                    ShutdownBlockReasonDestroy(handle);
            }
            catch
            {
                // 忽略
            }
        }
    }
}
