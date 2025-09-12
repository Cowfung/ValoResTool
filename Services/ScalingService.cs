using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValoResTool.Services
{
    public static class ScalingService
    {
        public static void SetScalingConfig()
        {
            PsExecService.Initialize();

            string cmd = "for /f \"tokens=*\" %K in ('reg query \"HKLM\\SYSTEM\\CurrentControlSet\\Services\\nvlddmkm\\State\\DisplayDatabase\"') do " +
                         "(reg query \"%K\" /v ScalingConfig && reg add \"%K\" /v ScalingConfig /t REG_BINARY /d db01000010000000830000006f010000 /f)";

            try
            {
                PsExecService.RunAsSystem(cmd);
                Console.WriteLine("✅ ScalingConfig đã set thành Fullscreen thành công!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi set ScalingConfig: {ex.Message}");
            }
        }
        /// <summary>
        /// Set Scaling = 3 cho tất cả key GPU trong Registry
        /// </summary>
        public static void SetScalingFullScreen()
        {
            try
            {
                string configBase = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers\Configuration";
                using RegistryKey baseKey = Registry.LocalMachine.OpenSubKey(configBase, writable: true);

                if (baseKey == null)
                {
                    Console.WriteLine("❌ Không tìm thấy Registry gốc GraphicsDrivers\\Configuration");
                    return;
                }

                foreach (string subKeyName in baseKey.GetSubKeyNames())
                {
                    using RegistryKey subKey = baseKey.OpenSubKey(subKeyName, writable: true);
                    if (subKey == null) continue;

                    foreach (string childName in subKey.GetSubKeyNames())
                    {
                        using RegistryKey childKey = subKey.OpenSubKey(childName, writable: true);
                        if (childKey == null) continue;

                        // Cấp 2
                        if (childKey.GetValue("Scaling") != null)
                        {
                            childKey.SetValue("Scaling", 3, RegistryValueKind.DWord);
                           
                        }

                        // Cấp 3
                        foreach (string subChild in childKey.GetSubKeyNames())
                        {
                            using RegistryKey subChildKey = childKey.OpenSubKey(subChild, writable: true);
                            if (subChildKey?.GetValue("Scaling") != null)
                            {
                                subChildKey.SetValue("Scaling", 3, RegistryValueKind.DWord);
                               
                            }
                        }
                    }
                }
                Console.WriteLine("✅ ScalingMode đã được set thành Fullscreen thành công!");


            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Lỗi khi chỉnh Scaling: {ex.Message}");
            }
        }

       
    }
}
