using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValoResTool.Services
{
    public class ConfigService
    {
        public void UpdateConfig(string templatePath, IEnumerable<string> accountFolders, int width, int height)
        {
            if (!File.Exists(templatePath))
            {
                Console.WriteLine("❌ Không tìm thấy GameUserSettings.ini mẫu.");
                return;
            }

            var lines = File.ReadAllLines(templatePath);
            var modified = new List<string>();
            bool inSection = false;

            foreach (var raw in lines)
            {
                string line = raw;

                if (line == "[/Script/ShooterGame.ShooterGameUserSettings]") inSection = true;
                else if (line.StartsWith("[") && line != "[/Script/ShooterGame.ShooterGameUserSettings]") inSection = false;

                if (inSection)
                {
                    if (line.StartsWith("ResolutionSizeX=")) line = $"ResolutionSizeX={width}";
                    else if (line.StartsWith("ResolutionSizeY=")) line = $"ResolutionSizeY={height}";
                    else if (line.StartsWith("LastUserConfirmedResolutionSizeX=")) line = $"LastUserConfirmedResolutionSizeX={width}";
                    else if (line.StartsWith("LastUserConfirmedResolutionSizeY=")) line = $"LastUserConfirmedResolutionSizeY={height}";
                    else if (line.StartsWith("bShouldLetterbox=")) line = "bShouldLetterbox=False";
                    else if (line.StartsWith("bLastConfirmedShouldLetterbox=")) line = "bLastConfirmedShouldLetterbox=False";
                    else if (line.StartsWith("LastConfirmedFullscreenMode=")) line = "LastConfirmedFullscreenMode=2";
                }

                modified.Add(line);
            }

            foreach (var folder in accountFolders)
            {
                string target = Path.Combine(folder, "WindowsClient");
                if (!Directory.Exists(target))
                    Directory.CreateDirectory(target);

                string configPath = Path.Combine(target, "GameUserSettings.ini");
                File.WriteAllLines(configPath, modified, System.Text.Encoding.UTF8);
                Console.WriteLine($"✅ Đã cập nhật config: {configPath}");
            }
        }
    }
}
