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

        public bool SetResolution(int width, int height, int hz)
        {
            if (!File.Exists(_qresPath))
            {
                Console.WriteLine("❌ Không tìm thấy QRes.exe.");
                return false;
            }

            var proc = new ProcessStartInfo
            {
                FileName = _qresPath,
                Arguments = $"/x:{width} /y:{height} /r:{hz}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(proc))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (output.Contains("Error", StringComparison.OrdinalIgnoreCase) ||
                    error.Contains("Error", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("❌ Độ phân giải / tần số quét không được hỗ trợ, vui lòng chọn lại.");
                    return false;
                }

                Console.WriteLine($"✅ Đã đổi độ phân giải sang {width}x{height} @{hz}Hz.");
                return true;
            }
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
