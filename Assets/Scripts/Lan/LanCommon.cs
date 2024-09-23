using System.Linq;
using System.Net;
using System.Net.Sockets;

public static class LanCommon
{
    public const ushort LanPort = 7777;

    public static string GetLocalIP()
        => Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork)?.ToString();
}
