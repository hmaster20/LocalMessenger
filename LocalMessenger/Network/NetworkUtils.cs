using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using LocalMessenger.Utilities;

namespace LocalMessenger.Core.Network
{
    public static class NetworkUtils
    {
        public static string GetLocalIPAddress()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                               n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                               !n.Name.Contains("Virtual") &&
                               !n.Name.Contains("VMnet") &&
                               !n.Name.Contains("VBox"));

                foreach (var networkInterface in networkInterfaces)
                {
                    var ipProps = networkInterface.GetIPProperties();
                    var ipv4Address = ipProps.UnicastAddresses
                        .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork &&
                                   a.Address.ToString().StartsWith("192.168."))
                        .Select(a => a.Address.ToString())
                        .FirstOrDefault();

                    if (ipv4Address != null)
                    {
                        Logger.Log($"Found local IP address: {ipv4Address}");
                        return ipv4Address;
                    }
                }

                Logger.Log("No suitable 192.168.x.x IP address found, falling back to first available IPv4 address");
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var fallbackAddress = host.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString();

                if (fallbackAddress == null)
                {
                    Logger.Log("No IPv4 address found");
                    throw new InvalidOperationException("No valid IPv4 address found");
                }

                return fallbackAddress;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error determining local IP address: {ex.Message}");
                throw;
            }
        }
    }
}