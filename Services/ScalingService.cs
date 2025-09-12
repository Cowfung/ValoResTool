using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ValoResTool.Services
{
    public static class ScalingService
    {

        public static bool SetScalingConfig()
        {
            PsExecService.Initialize();
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
                        string fullKey = $@"HKLM\{baseKey}\{subKeyName}";
                        using RegistryKey subKey = root.OpenSubKey(subKeyName);
                        if (subKey?.GetValue("ScalingConfig") is byte[] val)
                        {
                            byte[] target = new byte[] { 0xDB, 0x01, 0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x83, 0x00, 0x00, 0x00, 0x6F, 0x01, 0x00, 0x00 };
                            if (!val.SequenceEqual(target))
                            {
                                PsExecService.RunAsSystem($"reg add \"{fullKey}\" /v ScalingConfig /t REG_BINARY /d db01000010000000830000006f010000 /f");
                                changed = true;
                            }
                        }
                        else
                        {
                            // Nếu chưa có ScalingConfig thì add luôn
                            PsExecService.RunAsSystem($"reg add \"{fullKey}\" /v ScalingConfig /t REG_BINARY /d db01000010000000830000006f010000 /f");
                            changed = true;
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
