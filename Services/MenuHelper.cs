using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ValoResTool.Services
{
    public class MenuHelper
    {
        public static (int, int) ChooseResolution()
        {
            Console.WriteLine("\nChọn độ phân giải 4:3:");
            Console.WriteLine("1 - 1440 x 1080");
            Console.WriteLine("2 - 1280 x 960");
            Console.WriteLine("3 - 1024 x 768");
            Console.Write("👉 Nhập số bạn chọn: ");
            string choice = Console.ReadLine();

            return choice switch
            {
                "1" => (1440, 1080),
                "2" => (1280, 960),
                "3" => (1024, 768),
                _ => (1440, 1080)
            };
        }

        public static int ChooseHz()
        {
            Console.WriteLine("\nChọn tần số quét:");
            Console.WriteLine("1 - 60Hz | 2 - 75Hz | 3 - 100Hz");
            Console.WriteLine("4 - 144Hz | 5 - 165Hz | 6 - 240Hz | 7 - 360Hz");
            Console.Write("👉 Nhập số bạn chọn: ");
            string choice = Console.ReadLine();

            return choice switch
            {
                "2" => 75,
                "3" => 100,
                "4" => 144,
                "5" => 165,
                "6" => 240,
                "7" => 360,
                _ => 60
            };
        }

        public static async Task ShowActionMenu(
       string[] userFolders,
       string riotExe,
       RiotService riotService,
       ResolutionService resolutionService,
       ConfigService configService,
       string iniTemplatePath,
       int resX,
       int resY)
        {
            while (true)
            {
                Console.WriteLine("\nBạn muốn làm gì tiếp theo?");
                Console.WriteLine("1 - Lỗi hình ảnh ,Tắt - Reset config và mở lại Valorant");
                Console.WriteLine("2 - Đổi tài khoản khác ");
                Console.WriteLine("3 - Khôi phục 1920x1080 và thoát");
                Console.Write("> ");

                string action = Console.ReadLine().Trim();

                if (action == "1")
                {
                    ProcessHelper.KillValorant();
                    configService.UpdateConfig(iniTemplatePath, userFolders, resX, resY);
                    var loginInfo = await riotService.CheckRiotLoginAsync();
                    if (loginInfo != null)
                    {
                      await riotService.LaunchValorantAsync(loginInfo.AppPort, loginInfo.RemotingAuthToken);

                        Console.WriteLine("✅ Valorant đã reset config & Riot Client đã mở lại.");
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Không lấy được thông tin login Riot Client. Hãy mở game thủ công.");
                    }

                }
                else if (action == "2")
                {
                    ProcessHelper.KillValorant();
                    await riotService.LogoutRiotAsync();
                    if (!string.IsNullOrEmpty(riotExe) && File.Exists(riotExe))
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = riotExe,
                                UseShellExecute = true, // cho phép chạy file exe như click tay
                                WorkingDirectory = Path.GetDirectoryName(riotExe) // quan trọng, Riot hay crash nếu thiếu
                            });
                            Console.WriteLine("✅ Đã mở lại Riot Client để đổi tài khoản.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Không thể mở Riot Client: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠️ Không tìm thấy Riot Client exe. Hãy mở thủ công.");
                    }
                    break; // quay lại vòng while ngoài
                }
                else if (action == "3")
                {
                    ProcessHelper.KillValorant();
                    resolutionService.RestoreDefault();
                    Environment.Exit(0);
                }
            }
        }
    }
}
