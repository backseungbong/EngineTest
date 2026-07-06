using System.ComponentModel;
using System.Diagnostics;

namespace JHLib.Util.ProcessManagement
{
    public static class ProcessHelper
    {
        public static void Kill(string processName, int waitForExit = 1000, bool entireProcessTree = true)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return;

            foreach (var existing in Process.GetProcessesByName(processName))
            {
                try
                {
                    existing.Kill(entireProcessTree);
                    existing.WaitForExit(waitForExit);
                }
                catch (InvalidOperationException) { } // 이미 종료됨
                catch (Win32Exception) { } // 접근 권한 없음
                finally { existing.Dispose(); }
            }
        }
    }
}