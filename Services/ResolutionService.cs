using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValoResTool.Services
{
    public class ResolutionService
    {
        private readonly string _qresPath;

        public ResolutionService(string qresPath)
        {
            _qresPath = qresPath;
        }

        public void SetResolution(int width, int height, int hz)
        {
            if (!File.Exists(_qresPath))
            {
                Console.WriteLine("❌ Không tìm thấy QRes.exe.");
                return;
            }

            var proc = new ProcessStartInfo
            {
                FileName = _qresPath,
                Arguments = $"/x:{width} /y:{height} /r:{hz}",
                UseShellExecute = false
            };

            Process.Start(proc)?.WaitForExit();
            Console.WriteLine($"✅ Đã đổi độ phân giải sang {width}x{height} @{hz}Hz.");
        }

        public void RestoreDefault()
        {
            if (!File.Exists(_qresPath))
            {
                Console.WriteLine("❌ Không tìm thấy QRes.exe.");
                return;
            }

            var proc = new ProcessStartInfo
            {
                FileName = _qresPath,
                Arguments = "/x:1920 /y:1080", // không chỉ định Hz
                UseShellExecute = false
            };

            Process.Start(proc)?.WaitForExit();
            Console.WriteLine("✅ Đã khôi phục độ phân giải mặc định (1920x1080).");
        }
    }
}
