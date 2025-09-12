using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
            // 1. Thử registry key chuẩn
            using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"riotclient\shell\open\command"))
            {
                if (key != null)
                {
                    string value = key.GetValue(null) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        string exePath = value.Split('"')[1];
                        if (File.Exists(exePath))
                            return exePath;
                    }
                }
            }

            // 2. Thử các path phổ biến
            string[] commonPaths =
            {
        @"C:\Riot Games\Riot Client\RiotClientServices.exe",
        @"C:\Riot\RiotClient\RiotClientServices.exe",
        @"C:\ProgramData\Riot Games\RiotClientServices.exe"
    };

            foreach (var path in commonPaths)
            {
                if (File.Exists(path))
                    return path;
            }

            // 3. Quét Registry LocalServer32
            using (var clsidRoot = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Classes\CLSID"))
            {
                if (clsidRoot != null)
                {
                    foreach (var subKeyName in clsidRoot.GetSubKeyNames())
                    {
                        try
                        {
                            using (var subKey = clsidRoot.OpenSubKey(subKeyName + @"\LocalServer32"))
                            {
                                var value = subKey?.GetValue(null) as string;
                                if (!string.IsNullOrEmpty(value) &&
                                    value.Contains("RiotClientServices.exe", StringComparison.OrdinalIgnoreCase))
                                {
                                    string exePath = value.Trim().Trim('"');
                                    if (File.Exists(exePath))
                                        return exePath;
                                }
                            }
                        }
                        catch { /* ignore quyền lỗi */ }
                    }
                }
            }

           
            return null;
        }
        // 🔹 Lấy thông tin Riot Client từ log
        public async Task<(int AppPort, string Token)?> GetRiotClientInfoAsync(int maxRetry = 10)
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
                    Console.WriteLine("❌ Chưa tìm thấy log Riot Client. Đợi 10s...");
                    await Task.Delay(10000);
                    continue;
                }

                string logContent = File.ReadAllText(latestLog.FullName);
                var portMatch = Regex.Match(logContent, @"appPort:\s*(\d+)");
                var tokenMatch = Regex.Match(logContent, @"remotingAuthToken:\s*'([^']+)'");

                if (portMatch.Success && tokenMatch.Success)
                    return (int.Parse(portMatch.Groups[1].Value), tokenMatch.Groups[1].Value);

                await Task.Delay(10000);
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
                if (!response.IsSuccessStatusCode) return null;

                string content = await response.Content.ReadAsStringAsync();
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
            catch { /* lỗi connect -> chưa login */ }

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
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);

            // POST để bật Valorant
            var response = await client.PostAsync(
                $"https://127.0.0.1:{appPort}/product-launcher/v1/products/valorant/patchlines/live",
                null
            );

            if (response.IsSuccessStatusCode)
                Console.WriteLine("✅ Valorant đang được mở...");
            else
                Console.WriteLine($"❌ Lỗi khi bật Valorant: {response.StatusCode}");
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
                    await Task.Delay(3000);
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

    }
}
