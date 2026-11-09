using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace HelpMuslimWebApp.Api.Utils
{
    // Empreinte d'une adresse IP (jamais l'adresse elle-même) : suffit à repérer deux comptes créés depuis la même connexion.
    // IPv6 : seul le préfixe /64 compte (une box change souvent la fin de l'adresse).
    public static class IpFingerprint
    {
        public static string Hash(IPAddress ip, string? salt)
        {
            if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
            var value = ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6
                ? Convert.ToHexString(ip.GetAddressBytes()[..8])
                : ip.ToString();
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{salt ?? "muslink"}|{value}"));
            return Convert.ToHexString(bytes)[..32].ToLowerInvariant();
        }
    }
}
