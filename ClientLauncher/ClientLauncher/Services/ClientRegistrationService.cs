using ClientLauncher.Helpers;
using ClientLauncher.Models;
using ClientLauncher.Services.Interface;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClientLauncher.Services
{
    public class ClientRegistrationService : IClientRegistrationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _appBasePath;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private string? _machineId;

        public ClientRegistrationService()
        {
            _baseUrl = ConfigurationManager.AppSettings["ClientLauncherBaseUrl"] ?? "http://10.21.10.1:8102";
            _httpClient = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            _appBasePath = ConfigurationManager.AppSettings["AppsBasePath"] ?? @"C:\CompanyApps";
        }

        public string GetMachineId()
        {
            if (string.IsNullOrEmpty(_machineId))
            {
                _machineId = MachineInfoHelper.GetMachineId();
            }
            return _machineId;
        }

        public async Task<bool> RegisterMachineAsync()
        {
            try
            {
                Logger.Info("Starting machine registration...");

                var installedApps = GetInstalledApplications();

                var registerRequest = new ClientMachineRegisterDto
                {
                    MachineId = GetMachineId(),
                    MachineName = MachineInfoHelper.GetMachineName(),
                    ComputerName = Environment.MachineName,
                    UserName = MachineInfoHelper.GetUserName(),
                    DomainName = MachineInfoHelper.GetDomainName(),
                    IPAddress = MachineInfoHelper.GetIPAddress(),
                    MACAddress = MachineInfoHelper.GetMacAddress(),
                    OSVersion = MachineInfoHelper.GetOSVersion(),
                    OSArchitecture = MachineInfoHelper.GetOSArchitecture(),
                    CPUInfo = MachineInfoHelper.GetCPUInfo(),
                    TotalMemoryMB = MachineInfoHelper.GetTotalMemoryMB(),
                    AvailableDiskSpaceGB = MachineInfoHelper.GetAvailableDiskSpaceGB(),
                    InstalledApplications = installedApps,
                    ClientVersion = MachineInfoHelper.GetClientVersion(),
                    Location = "Auto-detected"
                };

                var json = JsonSerializer.Serialize(registerRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/clientmachine/register", content);

                if (response.IsSuccessStatusCode)
                {
                    Logger.Info("Machine registered successfully: {MachineId}", GetMachineId());
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Logger.Error("Failed to register machine. Status: {Status}, Error: {Error}", 
                        response.StatusCode, error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error during machine registration");
                return false;
            }
        }

        public async Task<bool> SendHeartbeatAsync()
        {
            try
            {
                var installedApps = GetInstalledApplications();

                var heartbeatRequest = new ClientMachineHeartbeatDto
                {
                    MachineId = GetMachineId(),
                    Status = "Online",
                    InstalledApplications = installedApps,
                    AvailableDiskSpaceGB = MachineInfoHelper.GetAvailableDiskSpaceGB()
                };

                var json = JsonSerializer.Serialize(heartbeatRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/clientmachine/heartbeat", content);

                if (response.IsSuccessStatusCode)
                {
                    Logger.Debug("Heartbeat sent successfully");
                    return true;
                }
                else
                {
                    Logger.Warn("Failed to send heartbeat. Status: {Status}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error sending heartbeat");
                return false;
            }
        }

        private List<string> GetInstalledApplications()
        {
            var installedApps = new List<string>();

            try
            {
                if (Directory.Exists(_appBasePath))
                {
                    var appDirectories = Directory.GetDirectories(_appBasePath);
                    foreach (var dir in appDirectories)
                    {
                        var appCode = Path.GetFileName(dir);
                        
                        // Skip Icons and temp directories
                        if (appCode != "Icons" && appCode != "Temp" && !appCode.StartsWith("."))
                        {
                            // Check if manifest exists to verify it's a valid app
                            var manifestPath = Path.Combine(dir, "App", "manifest.json");
                            if (File.Exists(manifestPath))
                            {
                                installedApps.Add(appCode);
                            }
                        }
                    }
                }

                Logger.Debug("Found {Count} installed applications", installedApps.Count);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error getting installed applications");
            }

            return installedApps;
        }
    }
}
