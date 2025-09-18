using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ValoResTool.Services
{
    public static class ConsoleHelper
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        public static void BringConsoleToFront()
        {
            IntPtr handle = GetConsoleWindow();
            if (handle != IntPtr.Zero)
            {
                SetForegroundWindow(handle);
            }
        }
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate handler, bool add);

        private static ConsoleEventDelegate handler;

        public static void PreventClose(Action onClose)
        {
            handler = new ConsoleEventDelegate(eventType =>
            {
                if (eventType == 2) // CTRL_CLOSE_EVENT
                {
                    onClose?.Invoke();
                    Environment.Exit(0);
                }
                return false;
            });
            SetConsoleCtrlHandler(handler, true);
        }
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        const uint WM_CLOSE = 0x0010;

        public static void CloseProcessGracefully(Process proc)
        {
            if (proc.MainWindowHandle != IntPtr.Zero)
            {
                PostMessage(proc.MainWindowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                proc.WaitForExit(5000); // đợi tối đa 5s
            }

            if (!proc.HasExited)
            {
                proc.Kill(); // fallback
            }
        }
    }
}
