using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValoResTool.Services
{
    public static  class ProcessHelper
    {
        public static void KillValorant()
        {
            try
            {
                var killProc = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = "/IM VALORANT-Win64-Shipping.exe /F",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(killProc)?.WaitForExit();
                // Chờ cho tiến trình thực sự biến mất
                while (Process.GetProcessesByName("VALORANT-Win64-Shipping").Length > 0)
                {
                    Task.Delay(5000).Wait();
                }
                Console.WriteLine("✅ Valorant đã được tắt thành công.");
            }
            catch
            {
                Console.WriteLine("⚠️ Không tắt được Valorant tự động, bạn có thể tắt thủ công.");
            }
        }
        public static readonly string[] RiotProcesses = { "RiotClientServices", "RiotClientUx", "RiotClientUxRender", "RiotClientElectron" };

        public static void KillRiotProcesses()
        {
            foreach (var p in RiotProcesses)
            {
                foreach (var proc in Process.GetProcessesByName(p))
                {
                    try
                    {
                        proc.Kill();
                        Console.WriteLine($"❌ Đã tắt {proc.ProcessName} (PID {proc.Id})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠ Không thể kill {proc.ProcessName}: {ex.Message}");
                    }
                }
            }
        }
    }
}
