using System.Diagnostics;
using System.Text;
using ValoResTool.Properties;
using ValoResTool.Services;
Console.OutputEncoding = Encoding.UTF8;

Console.WriteLine("==========================================");
Console.WriteLine("  CÔNG CỤ CHỈNH MÀN HÌNH VALORANT 4:3");
Console.WriteLine("==========================================");
Console.WriteLine();



// 🔧 Trích xuất file từ resource
string tempDir = Path.Combine(Path.GetTempPath(), "CowfungValoTool");
Directory.CreateDirectory(tempDir);
string qresPath = Path.Combine(tempDir, "QRes.exe");
string iniTemplatePath = Path.Combine(tempDir, "GameUserSettings.ini");

// Ghi file resource ra ổ tạm
File.WriteAllBytes(qresPath, Resources.QRes); // QRes.exe dạng byte[]
File.WriteAllBytes(iniTemplatePath, Resources.GameUserSettings); // GameUserSettings.ini cũng byte[]

// Sau khi có qresPath mới tạo service
var riotService = new RiotService();
var configService = new ConfigService();
var resolutionService = new ResolutionService(qresPath);

// Biến dùng chung
string baseConfig = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "VALORANT", "Saved", "Config");
while (true)
{


    // 🔹 Bước 1: Check Riot Client
    string riotExe = riotService.GetRiotClientExe();

    Console.WriteLine(riotExe ?? "Không tìm thấy Riot Client");
    if (riotExe != null)
    {
        Console.WriteLine($"👉 Đang thử mở Riot Client từ: {riotExe}");
        try
        {
            Process.Start(riotExe);
            Console.WriteLine("✅ Riot Client đã được mở.");

            string subject = await riotService.EnsureValorantAccountAsync(baseConfig);
            Console.WriteLine("🎉 Riot Client đã login & tài khoản Valorant đã sẵn sàng!");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi khi mở Riot Client: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("❌ Không tìm thấy Riot Client.");
    }



    // Chọn độ phân giải
    // 🔹 Bước 2: Người dùng chọn độ phân giải
    (int resX, int resY) = MenuHelper.ChooseResolution();
    int hz = MenuHelper.ChooseHz();
   
    // Tìm file GameUserSettings.ini gốc
    string templatePath = iniTemplatePath;
    if (!File.Exists(templatePath))
    {
        Console.WriteLine("Không tìm thấy GameUserSettings.ini mẫu.");
        Console.ReadKey(); return;
    }

   

    string configFolder = Directory.GetDirectories(baseConfig, "*-ap", SearchOption.TopDirectoryOnly)
        .FirstOrDefault(d => File.Exists(Path.Combine(d, "WindowsClient", "GameUserSettings.ini")));

    string[] userFolders = Directory.GetDirectories(baseConfig, "*-ap", SearchOption.TopDirectoryOnly);
    configService.UpdateConfig(iniTemplatePath, userFolders, resX, resY);

    if (userFolders.Length == 0)
    {
        Console.WriteLine("❌ Không tìm thấy thư mục tài khoản Valorant.");
        Console.ReadKey();
        return;
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
        var loginInfo = await riotService.CheckRiotLoginAsync(); // Lấy lại thông tin login để có appPort + token
        if (loginInfo != null)
        {
            await riotService.LaunchValorantAsync(loginInfo.AppPort, loginInfo.RemotingAuthToken);
        }
        else
        {
            Console.WriteLine("❌ Không thể lấy thông tin login Riot Client, vui lòng mở Riot Client và đăng nhập.");
        }

    }
    else
    {
        Console.WriteLine("Không tìm thấy QRes.exe.");
    }

    MenuHelper.ShowActionMenu(
    userFolders,
    riotExe,
    riotService,
    resolutionService,
    configService,
    iniTemplatePath,
    resX,
    resY
    );
    Console.TreatControlCAsInput = false;
    Console.CancelKeyPress += (s, e) => Environment.Exit(0);
    AppDomain.CurrentDomain.ProcessExit += (s, e) => Environment.Exit(0);
}

