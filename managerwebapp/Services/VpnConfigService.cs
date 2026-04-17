using managerwebapp.Constants;

namespace managerwebapp.Services;

public sealed class VpnConfigService
{
    public async Task<VpnConfigModel> LoadModelAsync(CancellationToken cancellationToken = default)
    {
        string content = await LoadEditorContentAsync(VpnConstants.VpnConfigFilePath, cancellationToken);
        return ParseModel(content);
    }

    public Task<string> BuildContentAsync(VpnConfigModel model, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(BuildContent(model));
    }

    public Task<bool> IsWireGuardInstalledAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(VpnConstants.WgPath) && File.Exists(VpnConstants.WgQuickPath));
    }

    public Task<VpnConfigFileState> LoadStateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        bool exists = File.Exists(VpnConstants.VpnConfigFilePath);
        string stateLabel = exists ? "OK" : "Missing";

        return Task.FromResult(new VpnConfigFileState(
            "wg0.conf",
            "Main WireGuard configuration for the manager control node.",
            VpnConstants.VpnConfigFilePath,
            stateLabel,
            exists));
    }

    public async Task<string> LoadEditorContentAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return string.Empty;
        }

        return await File.ReadAllTextAsync(filePath, cancellationToken);
    }

    public async Task SaveAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        await File.WriteAllTextAsync(filePath, NormalizeContent(content), cancellationToken);
    }

    private static string NormalizeContent(string content)
    {
        return content.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static VpnConfigModel ParseModel(string content)
    {
        VpnConfigModel model = new();
        string? currentSection = null;

        foreach (string rawLine in content.Split('\n'))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith(';'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1];
                continue;
            }

            int separatorIndex = line.IndexOf('=');
            if (separatorIndex < 0)
            {
                continue;
            }

            string key = line[..separatorIndex].Trim();
            string value = line[(separatorIndex + 1)..].Trim();

            if (string.Equals(currentSection, "Interface", StringComparison.OrdinalIgnoreCase))
            {
                switch (key)
                {
                    case "PrivateKey":
                        model.PrivateKey = value;
                        break;
                    case "Address":
                        model.Address = value;
                        break;
                    case "ListenPort":
                        model.ListenPort = value;
                        break;
                    case "DNS":
                        model.Dns = value;
                        break;
                }
            }
            else if (string.Equals(currentSection, "Peer", StringComparison.OrdinalIgnoreCase))
            {
                switch (key)
                {
                    case "PublicKey":
                        model.PeerPublicKey = value;
                        break;
                    case "PresharedKey":
                        model.PresharedKey = value;
                        break;
                    case "Endpoint":
                        model.Endpoint = value;
                        break;
                    case "AllowedIPs":
                        model.AllowedIps = value;
                        break;
                    case "PersistentKeepalive":
                        model.PersistentKeepalive = value;
                        break;
                }
            }
        }

        return model;
    }

    private static string BuildContent(VpnConfigModel model)
    {
        List<string> lines =
        [
            "[Interface]"
        ];

        AppendLine(lines, "PrivateKey", model.PrivateKey);
        AppendLine(lines, "Address", model.Address);
        AppendLine(lines, "ListenPort", model.ListenPort);
        AppendLine(lines, "DNS", model.Dns);

        lines.Add(string.Empty);
        lines.Add("[Peer]");
        AppendLine(lines, "PublicKey", model.PeerPublicKey);
        AppendLine(lines, "PresharedKey", model.PresharedKey);
        AppendLine(lines, "Endpoint", model.Endpoint);
        AppendLine(lines, "AllowedIPs", model.AllowedIps);
        AppendLine(lines, "PersistentKeepalive", model.PersistentKeepalive);

        return NormalizeContent(string.Join('\n', lines).TrimEnd() + "\n");
    }

    private static void AppendLine(ICollection<string> lines, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            lines.Add($"{key} = {value.Trim()}");
        }
    }
}

public sealed record VpnConfigFileState(
    string Title,
    string Description,
    string FilePath,
    string StateLabel,
    bool Exists);

public sealed class VpnConfigModel
{
    public string? PrivateKey { get; set; }
    public string? Address { get; set; }
    public string? ListenPort { get; set; }
    public string? Dns { get; set; }
    public string? PeerPublicKey { get; set; }
    public string? PresharedKey { get; set; }
    public string? Endpoint { get; set; }
    public string? AllowedIps { get; set; }
    public string? PersistentKeepalive { get; set; }
}
