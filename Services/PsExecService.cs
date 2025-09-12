using System;
using System.Diagnostics;
using System.IO;
using ValoResTool.Properties;

namespace ValoResTool.Services
{
    public static class PsExecService
    {
        private static string _psexecPath;

        public static void Initialize()
        {
            if (_psexecPath == null)
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "CowfungValoTool");
                Directory.CreateDirectory(tempDir);

                _psexecPath = Path.Combine(tempDir, "PsExec.exe");
                File.WriteAllBytes(_psexecPath, Resources.PsExec);
                
            }
        }

        // Chạy lệnh nào đó dưới SYSTEM, đợi xong và trả output
        public static void RunCommandAsSystem(string command)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _psexecPath,
                Arguments = $"-accepteula -s cmd /c \"{command}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (!string.IsNullOrEmpty(output))
                Console.WriteLine("[SYSTEM CMD OUTPUT] " + output.Trim());

            if (!string.IsNullOrEmpty(error))
                Console.WriteLine("[SYSTEM CMD ERROR] " + error.Trim());
        }
        public static string RunAsSystemAndCapture(string command)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _psexecPath,
                Arguments = $"-accepteula -s cmd /c \"{command}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (!string.IsNullOrEmpty(error))
                Console.WriteLine("[SYSTEM CMD ERROR] " + error.Trim());

            return output;
        }
        public static void RunAsSystem(string command)
        {
            var psi = new ProcessStartInfo
            {
                FileName = _psexecPath,
                Arguments = $"-accepteula -s cmd /c \"{command}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            proc.WaitForExit();
        }
    }
}
