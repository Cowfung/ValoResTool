using Microsoft.Win32;
using System.Diagnostics;
using System.Text;
using ValoResTool.Models;
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

bool running = true;

ConsoleHelper.PreventClose(() =>
{
    Console.WriteLine("👋 Tool đã thoát.");
    KillRiotProcesses();
    running = false;
});

AppDomain.CurrentDomain.ProcessExit += (s, e) =>
{
    KillRiotProcesses();
    running = false;
};
static void KillRiotProcesses()
{
    string[] riotProcesses =
    {
        "RiotClientServices",
        "RiotClientUx",
        "RiotClientUxRender",
        "RiotClientElectron"
    };

    foreach (var p in riotProcesses)
    {
        foreach (var proc in Process.GetProcessesByName(p))
        {
            try
            {
                proc.Kill();
                Console.WriteLine($"❌ Đã tắt {proc.ProcessName} (PID {proc.Id})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Không thể kill {proc.ProcessName}: {ex.Message}");
            }
        }
    }
}

while (running && !Environment.HasShutdownStarted)
{
    string? riotExe = riotService.GetRiotClientExe();
    RiotLoginInfo? loginInfo = null;
    if (riotExe == null)
    {
        Console.WriteLine("❌ Riot Client chưa được mở lần nào. Vui lòng mở Riot Client thủ công (chỉ cần mở một lần).");
        Console.WriteLine("⏳ Đang chờ bạn mở Riot Client...");

        string[] riotProcesses =
        {
        "RiotClientServices",
        "RiotClientUx",
        "RiotClientUxRender",
        "RiotClientElectron"
    };

        while (true)
        {
            var proc = riotProcesses
                .SelectMany(p => Process.GetProcessesByName(p))
                .FirstOrDefault();

            if (proc != null)
            {
                Console.WriteLine("✅ Riot Client đã được mở, tiếp tục...");

                try
                {
                    // 🔑 Lấy exePath trực tiếp từ process thay vì registry
                    riotExe = proc.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(riotExe))
                    {
                      
                        try
                        {
                            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\MyTool"))
                            {
                                key.SetValue("RiotExePath", riotExe);
                            }
                           
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠ Không lưu được RiotExePath: {ex.Message}");
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("⚠ Không lấy được exePath từ process, sẽ chỉ dùng detect process.");
                }
                break;
            }

            await Task.Delay(2000);
        }
    }

    else
    {
        try
        {
            string[] riotProcesses =
            {
            "RiotClientServices",
            "RiotClientUx",
            "RiotClientUxRender",
            "RiotClientElectron"
        };

            bool riotRunning = riotProcesses.Any(p => Process.GetProcessesByName(p).Any());

            if (!riotRunning)
            {
                // Riot Client chưa chạy → mở mới
                Process.Start(riotExe);
                Console.WriteLine("✅ Riot Client đã được mở.");
            }
            else
            {
                // Riot Client đang chạy → thử login
                loginInfo = await riotService.CheckRiotLoginAsync();
                if (loginInfo == null)
                {
                    Console.WriteLine("⚠ Riot Client chạy nhưng chưa login, sẽ khởi động lại...");

                    // Kill toàn bộ process Riot Client
                    foreach (var p in riotProcesses)
                    {
                        foreach (var proc in Process.GetProcessesByName(p))
                            proc.Kill();
                    }

                    await Task.Delay(2000); // chờ nó tắt hẳn

                    Process.Start(riotExe);
                    Console.WriteLine("🔄 Đã khởi động lại Riot Client.");
                }
                else
                {
                    Console.WriteLine("⚠ Riot Client đã chạy & login sẵn, bỏ qua mở lại.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Lỗi khi mở Riot Client: {ex.Message}");
        }
    }
  
    string subject = await riotService.EnsureValorantAccountAsync(baseConfig);
    Console.WriteLine("🎉 Riot Client đã login & tài khoản Valorant đã sẵn sàng!");
    // Chọn độ phân giải
    // 🔹 Bước 2: Người dùng chọn độ phân giải
    bool success = false;
    int resX = 0, resY = 0, hz = 0;

    while (!success)
    {
        (resX, resY) = MenuHelper.ChooseResolution();
        hz = MenuHelper.ChooseHz();

        success = resolutionService.SetResolution(resX, resY, hz);
    }

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
    if (loginInfo != null)
    {
        await riotService.LaunchValorantAsync(loginInfo.AppPort, loginInfo.RemotingAuthToken);
    }
    else
    {
        // 🔄 Thử check lại lần nữa sau khi đổi độ phân giải
        loginInfo = await riotService.CheckRiotLoginAsync();
        if (loginInfo != null)
        {
            await riotService.LaunchValorantAsync(loginInfo.AppPort, loginInfo.RemotingAuthToken);
        }
        else
        {
            Console.WriteLine("❌ Không thể lấy thông tin login Riot Client, vui lòng mở Riot Client và đăng nhập.");
        }
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

}

