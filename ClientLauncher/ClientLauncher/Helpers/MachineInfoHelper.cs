using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace ClientLauncher.Helpers
{
    public static class MachineInfoHelper
    {
        /// <summary>
        /// Generate unique machine ID based on hardware info
        /// </summary>
        public static string GetMachineId()
        {
            try
            {
                // Combine multiple identifiers for uniqueness
                var macAddress = GetMacAddress();
                var machineName = Environment.MachineName;
                var userName = Environment.UserName;

                var combined = $"{machineName}-{userName}-{macAddress}";

                // Create hash for consistent ID
                using (var sha256 = SHA256.Create())
                {
                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                    return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 32);
                }
            }
            catch
            {
                // Fallback to machine name + domain
                return $"{Environment.MachineName}-{Environment.UserDomainName}".GetHashCode().ToString("X");
            }
        }

        public static string GetMachineName()
        {
            return Environment.MachineName;
        }

        public static string GetUserName()
        {
            return Environment.UserName;
        }

        public static string GetDomainName()
        {
            return Environment.UserDomainName;
        }

        public static string GetIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return "127.0.0.1";
            }
            catch
            {
                return "Unknown";
            }
        }

        public static string GetMacAddress()
        {
            try
            {
                var nic = NetworkInterface.GetAllNetworkInterfaces()
                    .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                                        n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                return nic?.GetPhysicalAddress().ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        public static string GetOSVersion()
        {
            return Environment.OSVersion.ToString();
        }

        public static string GetOSArchitecture()
        {
            return Environment.Is64BitOperatingSystem ? "x64" : "x86";
        }

        public static string GetCPUInfo()
        {
            return Environment.ProcessorCount + " Processors";
        }

        public static long GetTotalMemoryMB()
        {
            // Approximate based on environment
            return 8192; // Default 8GB, actual detection requires WMI
        }

        public static long GetAvailableDiskSpaceGB()
        {
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed);

                long totalAvailable = 0;
                foreach (var drive in drives)
                {
                    totalAvailable += drive.AvailableFreeSpace;
                }

                return totalAvailable / (1024 * 1024 * 1024); // Convert to GB
            }
            catch
            {
                return 0;
            }
        }

        public static string GetClientVersion()
        {
            try
            {
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }
    }
}
