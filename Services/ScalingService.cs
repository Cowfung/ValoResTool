using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ValoResTool.Services
{
    public static class ScalingService
    {

       
            // hex data as string for reg add (no commas, lower/upper both ok)
            private const string TargetHex = "db01000010000000830000006f010000";

            public static bool SetScalingConfig()
            {
                bool changed = false;
                string baseKey = @"SYSTEM\CurrentControlSet\Services\nvlddmkm\State\DisplayDatabase";

                try
                {
                    using (RegistryKey root = Registry.LocalMachine.OpenSubKey(baseKey))
                    {
                        if (root == null)
                        {
                            Console.WriteLine("ℹ️ Không tìm thấy DisplayDatabase");
                            return false;
                        }

                        foreach (string subKeyName in root.GetSubKeyNames())
                        {
                            string regPath = $@"HKLM\{baseKey}\{subKeyName}";
                            using RegistryKey subKey = root.OpenSubKey(subKeyName);
                        if (subKey?.GetValue("ScalingConfig") is byte[] val)
                        {
                            byte[] target = new byte[] { 0xDB, 0x01, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x83, 0x00, 0x00, 0x00, 0x6F, 0x01, 0x00, 0x00 };
                            if (!val.SequenceEqual(target))
                            {
                                Console.WriteLine($"⚙️ Updating ScalingConfig at {regPath}");
                                bool ok = CreateRunAndDeleteTaskForRegAdd(regPath, "ScalingConfig", TargetHex, out string taskOutput);
                                Console.WriteLine(taskOutput);
                                if (ok) changed = true;
                            }
                           
                        }

                       
                        }
                    }

                    if (changed)
                        Console.WriteLine("✅ ScalingConfig đã set thành công!");
                    else
                        Console.WriteLine("ℹ️ Không có key nào cần chỉnh, ScalingConfig giữ nguyên.");

                    return changed;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Lỗi khi set ScalingConfig: {ex.Message}");
                    return false;
                }
            }

        // Create a scheduled task (unique name), run it, wait for completion, then delete it.
        // Returns true if reg add reported success (exit code 0)
        private static bool CreateRunAndDeleteTaskForRegAdd(string regKeyPath, string valueName, string hexData, out string output)
        {
            string taskName = $"Cowfung_SetScaling_{Guid.NewGuid():N}";

            // Nội dung .bat
            string batContent = $"reg add \"{regKeyPath}\" /v {valueName} /t REG_BINARY /d {hexData} /f";
            string batPath = Path.Combine(Path.GetTempPath(), taskName + ".bat");
            File.WriteAllText(batPath, batContent, Encoding.ASCII);

            // Task sẽ gọi .bat
            string createArgs = $"/Create /TN \"{taskName}\" /SC ONCE /ST 23:59 /F /RL HIGHEST /RU SYSTEM /TR \"{batPath}\"";

            var createResult = RunProcessCapture("schtasks", createArgs, 30_000);
            if (createResult.ExitCode != 0)
            {
                output = $"[CreateTaskFailed] {createResult.StdErr}";
                return false;
            }

            var runResult = RunProcessCapture("schtasks", $"/Run /TN \"{taskName}\"", 30_000);
            Thread.Sleep(2000);

            // verify registry
            bool success = false;
            try
            {
                byte[] expected = HexStringToByteArray(hexData);
                const string prefix = "HKLM\\";
                if (regKeyPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    string subPath = regKeyPath.Substring(prefix.Length);
                    using var verifyKey = Registry.LocalMachine.OpenSubKey(subPath);
                    if (verifyKey?.GetValue(valueName) is byte[] got)
                        success = got.SequenceEqual(expected);
                }
            }
            catch { }

            RunProcessCapture("schtasks", $"/Delete /TN \"{taskName}\" /F", 15_000);
            try { File.Delete(batPath); } catch { }

            output = $"verify: {(success ? "ok" : "failed")}";
            return success;
        }


        // Run a process and capture stdout/stderr with timeout (ms)
        private static (int ExitCode, string StdOut, string StdErr) RunProcessCapture(string fileName, string arguments, int timeoutMs)
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return (-1, "", "Process start failed");

                string outStr = proc.StandardOutput.ReadToEnd();
                string errStr = proc.StandardError.ReadToEnd();

                bool exited = proc.WaitForExit(timeoutMs);
                if (!exited)
                {
                    try { proc.Kill(); } catch { }
                    return (-2, outStr, errStr + " (timeout)");
                }

                return (proc.ExitCode, outStr, errStr);
            }

            

            private static byte[] HexStringToByteArray(string hex)
            {
                if (hex.Length % 2 == 1) throw new ArgumentException("Hex string must have even length");
                int len = hex.Length / 2;
                byte[] res = new byte[len];
                for (int i = 0; i < len; i++)
                    res[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
                return res;
            }
        

        /// <summary>
        /// Set Scaling = 3 cho tất cả key GPU trong Registry
        /// </summary>
        public static bool SetScalingFullScreen()
        {

            try
            {
                string configBase = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Configuration";
                using RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(configBase, writable: true);

                if (baseKey == null)
                {   
                    Console.WriteLine("❌ Không tìm thấy Registry gốc GraphicsDrivers\\Configuration");
                    return false;
                }
                bool changed = false;

                foreach (string subKeyName in baseKey.GetSubKeyNames())
                {

                    using RegistryKey subKey = baseKey.OpenSubKey(subKeyName, writable: true);
                    if (subKey == null) continue;

                    foreach (string childName in subKey.GetSubKeyNames())
                    {
                        using RegistryKey childKey = subKey.OpenSubKey(childName, writable: true);
                        if (childKey == null) continue;

                        // Cấp 2
                        if (childKey.GetValue("Scaling") is int val && val != 3)
                        {
                            childKey.SetValue("Scaling", 3, RegistryValueKind.DWord);
                            changed = true;
                        }

                        // Cấp 3
                        foreach (string subChild in childKey.GetSubKeyNames())
                        {
                            using RegistryKey subChildKey = childKey.OpenSubKey(subChild, writable: true);
                            if (subChildKey?.GetValue("Scaling") is int subVal && subVal != 3)
                            {
                                subChildKey.SetValue("Scaling", 3, RegistryValueKind.DWord);
                                changed = true;
                            }
                        }
                    }
                    
                }
                if (changed)
                    Console.WriteLine("✅ ScalingMode đã được set thành công!");
                else
                    Console.WriteLine("ℹ️ Không có key nào cần chỉnh, ScalingMode giữ nguyên.");

                return changed;



            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi chỉnh Scaling: {ex.Message}");
                return false;
            }
        }

       
    }
}
