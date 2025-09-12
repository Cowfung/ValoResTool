using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ValoResTool.Properties;

namespace ValoResTool.Services
{
    public static class GpuService
    {
        /// <summary>
        /// Giải nén DevCon.exe từ resource ra temp folder
        /// </summary>
        /// <returns>Đường dẫn DevCon.exe</returns>
        public static string ExtractDevCon()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "CowfungValoTool");
            Directory.CreateDirectory(tempDir);

            string devconPath = Path.Combine(tempDir, "devcon.exe");
            Console.WriteLine($"DevCon extracted to: {devconPath}");
            File.WriteAllBytes(devconPath, Resources.devcon); // resource byte[]

            return devconPath;
        }

        /// <summary>
        /// Tìm tất cả GPU NVIDIA trên máy, trả về wildcard phù hợp
        /// </summary>
        /// <param name="devconPath"></param>
        /// <returns>Danh sách wildcard GPU</returns>
        public static string[] GetNvidiaGpuWildcards(string devconPath)
        {
            var proc = new Process();
            proc.StartInfo.FileName = devconPath;
            proc.StartInfo.Arguments = "find *10DE*";
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();

            string output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            // Lọc các GPU NVIDIA
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var nvidiaIds = lines
                 .Where(l => l.Contains("NVIDIA"))
                 .Select(l =>
                 {
                     var instanceId = l.Split(':')[0].Trim();

                     // Cắt chỉ lấy đến DEV_xxxx
                     var devIndex = instanceId.IndexOf("&DEV_");
                     if (devIndex > 0)
                     {
                         var prefix = instanceId.Substring(0, devIndex + 9); // giữ tới DEV_xxxx
                         return prefix + "*";
                     }

                     return null;
                 })
                 .Where(id => !string.IsNullOrEmpty(id))
                 .Distinct()
                 .ToArray();

            return nvidiaIds;
        }

        /// <summary>
        /// Reset GPU: disable -> chờ -> enable
        /// </summary>
        /// <param name="devconPath"></param>
        public static void ResetGpu()
        {
            string devconPath = ExtractDevCon();
            string[] gpus = GetNvidiaGpuWildcards(devconPath);
            if (gpus.Length == 0)
            {
                Console.WriteLine("Không tìm thấy GPU NVIDIA nào trên máy!");
                return;
            }
           

            Console.WriteLine($"Tìm thấy {gpus.Length} GPU NVIDIA:");
            foreach (var gpu in gpus)
            {
                Console.WriteLine($"Resetting GPU: {gpu}");
                Console.WriteLine("Disabling GPU...");
                RunDevCon(devconPath, $"disable \"{gpu}\"");
                Console.WriteLine("Waiting 5 seconds...");
                System.Threading.Thread.Sleep(5000);
                Console.WriteLine("Enabling GPU...");
                RunDevCon(devconPath, $"enable \"{gpu}\"");
            }
        }

        private static void RunDevCon(string devconPath, string arguments)
        {
            var proc = new Process();
            proc.StartInfo.FileName = devconPath;
            proc.StartInfo.Arguments = arguments;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(output))
                Console.WriteLine("Output: " + output.Trim());

            if (!string.IsNullOrEmpty(error))
                Console.WriteLine("Error: " + error.Trim());
            //Console.WriteLine(proc.StandardOutput.ReadToEnd());
            proc.WaitForExit();
        }
    }
}
