using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ValoResTool.Models;

namespace ValoResTool.Services
{
    public class RiotService
    {
        public string? GetRiotClientExe()
        {
            // Ưu tiên 1: RiotClientInstalls.json
            string jsonPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                           "Riot Games", "RiotClientInstalls.json");
            if (File.Exists(jsonPath))
            {
                var doc = JsonDocument.Parse(File.ReadAllText(jsonPath));
                if (doc.RootElement.TryGetProperty("rc_default", out var pathProp))
                {
                    string? exePath = pathProp.GetString();
                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                        return exePath;
                }
            }

            using (var storeKey = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store"))
            {
                if (storeKey != null)
                {
                    foreach (var valueName in storeKey.GetValueNames())
                    {
                        if (valueName.EndsWith("RiotClientServices.exe", StringComparison.OrdinalIgnoreCase) &&
                            File.Exists(valueName))
                        {
                            return valueName; // valueName chính là full path exe
                        }
                    }
                }
            }


            return null;
        }

        // 🔹 Lấy thông tin Riot Client từ log
        public async Task<(int AppPort, string Token)?> GetRiotClientInfoAsync(int maxRetry = 12)
        {
            string logFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Riot Games", "Riot Client", "Logs", "Riot Client Electron Logs");

            for (int i = 0; i < maxRetry; i++)
            {
                var files = new DirectoryInfo(logFolder).GetFiles("*.log");
                var latestLog = files.OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                if (latestLog == null)
                {
                    Console.WriteLine("❌ Chưa tìm thấy log Riot Client. Đợi 5s...");
                    await Task.Delay(5000);
                    continue;
                }

                string logContent = File.ReadAllText(latestLog.FullName);
                var portMatch = Regex.Match(logContent, @"appPort:\s*(\d+)");
                var tokenMatch = Regex.Match(logContent, @"remotingAuthToken:\s*'([^']+)'");

                if (portMatch.Success && tokenMatch.Success)
                    return (int.Parse(portMatch.Groups[1].Value), tokenMatch.Groups[1].Value);

                await Task.Delay(5000);
            }

            Console.WriteLine("❌ Không lấy được thông tin Riot Client sau nhiều lần thử.");
            return null;
        }
        // 🔹 Kiểm tra login
        public async Task<RiotLoginInfo?> CheckRiotLoginAsync()
        {
            var info = await GetRiotClientInfoAsync();
            if (info == null) return null;

            try
            {
                using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
                using var client = new HttpClient(handler);
                string authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"riot:{info.Value.Token}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

                var response = await client.GetAsync($"https://127.0.0.1:{info.Value.AppPort}/rso-auth/v1/authorization");
                string content = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                    return null;

              
                var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("subject", out var subjectProp))
                {
                    return new RiotLoginInfo
                    {
                        Subject = subjectProp.GetString(),
                        AppPort = info.Value.AppPort,
                        RemotingAuthToken = info.Value.Token
                    };
  
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Không kết nối được Riot Client API: {ex.Message}");
            }

            return null;
        }
        // Hàm bật Valorant lần đầu (chỉ mở game, không chờ folder, không tắt game)
        public async Task LaunchValorantAsync(int appPort, string remotingAuthToken)
        {
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"riot:{remotingAuthToken}"));
            using var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (msg, cert, chain, errs) => true;
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", authValue);

            // POST để bật Valorant
            var url = $"https://127.0.0.1:{appPort}/product-launcher/v1/products/valorant/patchlines/live";
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                var response = await client.PostAsync(url, null);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Valorant đang được mở...");
                    return;
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    Console.WriteLine("⚠ Valorant đang chạy hoặc đã được khởi động, bỏ qua.");
                    return;
                }

                Console.WriteLine($"❌ Thử {attempt}: Lỗi khi bật Valorant: {response.StatusCode}");
                await Task.Delay(3000);
            }
        }
        // 🔹 Logout Riot Client
        public async Task<bool> LogoutRiotAsync()
        {
            var info = await GetRiotClientInfoAsync();
            if (info == null) return false;

            try
            {
                using var handler = new HttpClientHandler { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
                using var client = new HttpClient(handler);
                string authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"riot:{info.Value.Token}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);

                var response = await client.DeleteAsync($"https://127.0.0.1:{info.Value.AppPort}/rso-auth/v1/authorization");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Riot Client đã logout thành công!");
                    return true;
                }
                Console.WriteLine($"❌ Lỗi khi logout Riot Client: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Lỗi khi logout Riot Client: {ex.Message}");
            }
            return false;
        }
        public async Task<string> EnsureValorantAccountAsync(string baseConfig)
        {
            // Trước khi check login
            await EnsureRiotClientFullUI();
            RiotLoginInfo? loginInfo = null;
            while (loginInfo == null)
            {
                loginInfo = await CheckRiotLoginAsync();
               
                if (loginInfo == null)
                {
                    Console.WriteLine("⏳ Chưa login Riot Client, thử lại sau 10s...");
                    await Task.Delay(10000); // thay vì chờ người dùng bấm Enter
                }
               
            }
            string subject = loginInfo.Subject;
          

            string subjectFolder = Path.Combine(baseConfig, $"{subject}-ap", "WindowsClient");
           

            if (!Directory.Exists(subjectFolder))
            {
                Console.WriteLine("⏳ Tài khoản Valorant chưa chơi trên PC này, tôi sẽ khởi chạy game qua Riot Client...");
                await LaunchValorantAsync(loginInfo.AppPort, loginInfo.RemotingAuthToken);
                // Chờ folder account xuất hiện
                while (!Directory.Exists(subjectFolder))
                {
                    Console.WriteLine("⏳ Đang chờ Valorant tạo thư mục account...");
                    await Task.Delay(5000);
                }

                // Tắt game bằng taskkill
                ProcessHelper.KillValorant();
                Console.WriteLine("🎉 Valorant res tool đã được khởi tạo thành công!");
            }
            else
            {
                Console.WriteLine("✅ Tài khoản Valorant đã chơi trên PC, tiếp tục bước setting...");
            }

            return subject;
        }
        private async Task EnsureRiotClientFullUI()
        {
            var exePath = GetRiotClientExe();
            if (exePath == null)
            {
                Console.WriteLine("❌ Không tìm thấy RiotClientServices.exe");
                return;
            }

            // Nếu đang chạy ở bootstrap mode thì kill trước
            foreach (var p in Process.GetProcessesByName("RiotClientServices"))
            {
                try
                {
                    p.Kill();
                    // Kiểm tra còn chạy không rồi mới WaitForExit
                    if (!p.HasExited)
                        p.WaitForExit(3000); // timeout 3 giây tránh treo
                }
                catch (InvalidOperationException)
                {
                    // Process đã thoát trước khi gọi -> bỏ qua
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠ Không thể kill RiotClientServices: {ex.Message}");
                }
            }

            Console.WriteLine("⚡ Đang khởi động Riot Client với UI đầy đủ...");
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "", // để trống = bật launcher UI thay vì bootstrap
                UseShellExecute = true
            });

            await Task.Delay(5000);
        }
      
      
    }
}
