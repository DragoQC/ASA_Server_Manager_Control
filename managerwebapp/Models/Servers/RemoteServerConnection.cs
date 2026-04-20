using System.Net;

namespace managerwebapp.Models.Servers;

public sealed record RemoteServerConnection(
    int Id,
    string VpnAddress,
    int? Port,
    string ApiKey)
{
    public string Host => NormalizeHost(VpnAddress);

    public string BaseUrl => Port.HasValue
        ? BuildBaseUrl(Host, Port.Value)
        : throw new InvalidOperationException($"Remote server '{Id}' has no configured port.");

    private static string NormalizeHost(string vpnAddress)
    {
        string trimmed = vpnAddress.Trim();
        int slashIndex = trimmed.IndexOf('/');
        return slashIndex >= 0 ? trimmed[..slashIndex] : trimmed;
    }

    private static string BuildBaseUrl(string host, int port)
    {
        if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            Uri uri = new(host, UriKind.Absolute);
            UriBuilder builder = new(uri)
            {
                Port = port
            };

            return builder.Uri.ToString().TrimEnd('/');
        }

        bool isIpAddress = IPAddress.TryParse(host, out _);
        string normalizedHost = host.Contains(':', StringComparison.Ordinal) && !host.StartsWith("[", StringComparison.Ordinal)
            ? $"[{host}]"
            : host;
        string scheme = isIpAddress ? "http" : "https";
        return $"{scheme}://{normalizedHost}:{port}";
    }
}
