using System.Diagnostics;
using System.Text;
using ValoResTool.Properties;

// 🔧 Trích xuất file từ resource
string tempDir = Path.Combine(Path.GetTempPath(), "CowfungValoTool");
Directory.CreateDirectory(tempDir);

string qresPath = Path.Combine(tempDir, "QRes.exe");
string iniTemplatePath = Path.Combine(tempDir, "GameUserSettings.ini");


File.WriteAllBytes(qresPath, Resources.QRes); // QRes.exe dạng byte[]
File.WriteAllBytes(iniTemplatePath, Resources.GameUserSettings); // GameUserSettings.ini cũng byte[]


Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("==========================================");
Console.WriteLine("  CÔNG CỤ CHỈNH MÀN HÌNH VALORANT 4:3");
Console.WriteLine("==========================================");
Console.WriteLine("- Game PHẢI được mở ít nhất 1 lần cho mỗi tài khoản mới.");
Console.WriteLine("- Nếu alt-tab hoặc lỗi hình ảnh → Thoát game + chạy lại tool.");
Console.WriteLine();

// Mở Facebook & Youtube
Console.Write("Bạn có muốn mở Facebook & YouTube? (y/n): ");
string openWeb = Console.ReadLine().Trim().ToLower();
if (openWeb == "y")
{
    string[] browsers = {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files\CocCoc\Browser\Application\browser.exe",
                @"C:\Program Files (x86)\CocCoc\Browser\Application\browser.exe"
            };

    string browserPath = browsers.FirstOrDefault(File.Exists);
    if (browserPath != null)
    {
        Process.Start(browserPath, "https://facebook.com https://youtube.com");
    }
    else
    {
        Console.WriteLine("Không tìm thấy Chrome hoặc Cốc Cốc.");
    }
}

// Chọn độ phân giải
Console.WriteLine("\nChọn độ phân giải 4:3:");
Console.WriteLine("1 - 1400 x 1080");
Console.WriteLine("2 - 1280 x 960");
Console.WriteLine("3 - 1024 x 768");
Console.Write("Nhập số bạn chọn (1/2/3): ");
string resChoice = Console.ReadLine();
int resX = 1400, resY = 1080;

switch (resChoice)
{
    case "1": resX = 1400; resY = 1080; break;
    case "2": resX = 1280; resY = 960; break;
    case "3": resX = 1024; resY = 768; break;
    default:
        Console.WriteLine("Lựa chọn không hợp lệ!");
        Console.ReadKey(); return;
}

// Chọn Hz
Console.WriteLine("\nChọn tần số quét:");
Console.WriteLine("1 - 60Hz");
Console.WriteLine("2 - 144Hz");
Console.WriteLine("3 - 240Hz");
Console.WriteLine("4 - 360Hz");
Console.Write("Nhập số bạn chọn (1/2/3/4): ");
string hzChoice = Console.ReadLine();
int hz = 60;
if (hzChoice == "2") hz = 144;
else if (hzChoice == "3") hz = 240;
else if (hzChoice == "4") hz = 360;

// Tìm file GameUserSettings.ini gốc
string templatePath = iniTemplatePath;
if (!File.Exists(templatePath))
{
    Console.WriteLine("Không tìm thấy GameUserSettings.ini mẫu.");
    Console.ReadKey(); return;
}

// Đọc và sửa config
var lines = File.ReadAllLines(templatePath);
bool inSection = false;
var modified = new List<string>();

foreach (var raw in lines)
{
    string line = raw;
    if (line == "[/Script/ShooterGame.ShooterGameUserSettings]") inSection = true;
    else if (line.StartsWith("[") && line != "[/Script/ShooterGame.ShooterGameUserSettings]") inSection = false;

    if (inSection)
    {
        if (line.StartsWith("ResolutionSizeX=")) line = $"ResolutionSizeX={resX}";
        else if (line.StartsWith("ResolutionSizeY=")) line = $"ResolutionSizeY={resY}";
        else if (line.StartsWith("LastUserConfirmedResolutionSizeX=")) line = $"LastUserConfirmedResolutionSizeX={resX}";
        else if (line.StartsWith("LastUserConfirmedResolutionSizeY=")) line = $"LastUserConfirmedResolutionSizeY={resY}";
        else if (line.StartsWith("bShouldLetterbox=")) line = $"bShouldLetterbox=False";
        else if (line.StartsWith("bLastConfirmedShouldLetterbox=")) line = $"bLastConfirmedShouldLetterbox=False";
        else if (line.StartsWith("LastConfirmedFullscreenMode=")) line = $"LastConfirmedFullscreenMode=2";
        else if (line.StartsWith("HDRDisplayOutputNits="))
        {
            modified.Add(line);
            modified.Add("Fullscreenmode=2");
            continue;
        }
    }

    modified.Add(line);
}

// Tìm thư mục Valorant config thực sự
string baseConfig = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "VALORANT", "Saved", "Config");

string configFolder = Directory.GetDirectories(baseConfig, "*-ap", SearchOption.TopDirectoryOnly)
    .FirstOrDefault(d => File.Exists(Path.Combine(d, "Windows", "GameUserSettings.ini")));

string[] userFolders = Directory.GetDirectories(baseConfig, "*-ap", SearchOption.TopDirectoryOnly);

if (userFolders.Length == 0)
{
    Console.WriteLine("❌ Không tìm thấy thư mục tài khoản Valorant.");
    Console.ReadKey();
    return;
}

foreach (var folder in userFolders)
{
    string targetFolder = Path.Combine(folder, "Windows");
    string configPath = Path.Combine(targetFolder, "GameUserSettings.ini");

    if (!Directory.Exists(targetFolder))
    {
        Directory.CreateDirectory(targetFolder);
    }

    File.WriteAllLines(configPath, modified, Encoding.UTF8);
    Console.WriteLine($"✅ Đã cập nhật: {configPath}");
}


// Gọi QRes

if (File.Exists(qresPath))
{
    var proc = new ProcessStartInfo
    {
        FileName = qresPath,
        Arguments = $"/x:{resX} /y:{resY} /r:{hz}",
        UseShellExecute = false
    };
    Process.Start(proc)?.WaitForExit();
    Console.WriteLine($"✅ Đã đổi độ phân giải sang {resX}x{resY} @{hz}Hz.");
}
else
{
    Console.WriteLine("Không tìm thấy QRes.exe.");
}

// Hỏi khôi phục
Console.WriteLine("\nMệt rồi à? Bấm Y để TRỞ VỀ 1920x1080 (y/n): ");
Console.Write("> "); // hiện dấu nhắc
string back = Console.ReadLine().ToLower();
if (back == "y")
{
    Process.Start(new ProcessStartInfo
    {
        FileName = qresPath,
        Arguments = "/x:1920 /y:1080",
        UseShellExecute = false
    })?.WaitForExit();
    Console.WriteLine("✅ Đã khôi phục độ phân giải về mặc định.");
}

Console.WriteLine("\nHoàn tất. Nhấn phím bất kỳ để thoát.");
Console.ReadKey();