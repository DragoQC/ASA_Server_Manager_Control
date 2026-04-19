using System.Security.Cryptography;
using managerwebapp.Data;
using managerwebapp.Data.Entities;
using managerwebapp.Models.Invitations;
using managerwebapp.Models.Vpn;
using Microsoft.EntityFrameworkCore;

namespace managerwebapp.Services;

public sealed class InvitationService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    VpnConfigService vpnConfigService,
    ClusterSettingsService clusterSettingsService)
{
    public async Task<IReadOnlyList<InvitationListItem>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<InvitationListItem> items = await dbContext.Invitations
            .Select(invitation => new InvitationListItem(
                invitation.Id,
                invitation.RemoteUrl,
                invitation.ClusterId,
                invitation.VpnAddress,
                invitation.InviteLink,
                invitation.InviteStatus,
                invitation.ValidationStatus,
                invitation.UsedAtUtc,
                invitation.LastSeenAtUtc,
                invitation.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return items
            .OrderByDescending(invitation => invitation.CreatedAtUtc)
            .ToList();
    }

    public async Task<InvitationFormModel> CreateDefaultFormAsync(CancellationToken cancellationToken = default)
    {
        VpnConfigModel currentConfig = await vpnConfigService.LoadConfiguredModelAsync(cancellationToken);
        Models.Cluster.ClusterSettingsModel clusterSettings = await clusterSettingsService.LoadAsync(cancellationToken);
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        int inviteCount = await dbContext.Invitations.CountAsync(cancellationToken);

        return new InvitationFormModel
        {
            ClusterId = clusterSettings.ClusterId,
            VpnAddress = GetNextVpnAddress(currentConfig.Address, inviteCount)
        };
    }

    public async Task<InviteRemoteServerRequest> BuildPreviewAsync(InvitationFormModel form, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(form.VpnAddress))
        {
            throw new InvalidOperationException("VPN address is required.");
        }

        string clusterId = await clusterSettingsService.LoadRequiredClusterIdAsync(cancellationToken);
        VpnConfigModel vpnConfig = await vpnConfigService.LoadConfiguredModelAsync(cancellationToken);
        SavedVpnKeyPair clientKeys = await vpnConfigService.LoadClientKeyPairAsync(cancellationToken);
        SavedVpnKeyPair serverKeys = await vpnConfigService.LoadServerKeyPairAsync(cancellationToken);

        InvitationEntity previewInvitation = new()
        {
            RemoteUrl = string.Empty,
            ClusterId = clusterId,
            VpnAddress = form.VpnAddress.Trim(),
            RemoteApiKey = "(generated on link creation)",
            OneTimeVpnKey = "preview",
            InviteLink = string.Empty,
            InviteStatus = "Preview",
            ValidationStatus = "Preview"
        };

        return BuildInviteRequest(previewInvitation, vpnConfig, clientKeys, serverKeys);
    }

    public async Task<InvitationListItem> CreateInvitationLinkAsync(InvitationFormModel form, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(form.VpnAddress))
        {
            throw new InvalidOperationException("VPN address is required.");
        }

        string clusterId = await clusterSettingsService.LoadRequiredClusterIdAsync(cancellationToken);
        VpnConfigModel vpnConfig = await vpnConfigService.LoadConfiguredModelAsync(cancellationToken);
        SavedVpnKeyPair clientKeys = await vpnConfigService.LoadClientKeyPairAsync(cancellationToken);
        SavedVpnKeyPair serverKeys = await vpnConfigService.LoadServerKeyPairAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(vpnConfig.Endpoint))
        {
            throw new InvalidOperationException("VPN endpoint is required before creating invitation links.");
        }

        if (string.IsNullOrWhiteSpace(vpnConfig.ListenPort))
        {
            throw new InvalidOperationException("VPN listen port is required before creating invitation links.");
        }

        if (string.IsNullOrWhiteSpace(vpnConfig.Dns))
        {
            throw new InvalidOperationException("VPN DNS is required before creating invitation links.");
        }

        if (string.IsNullOrWhiteSpace(vpnConfig.AllowedIps))
        {
            throw new InvalidOperationException("VPN allowed IPs are required before creating invitation links.");
        }

        if (string.IsNullOrWhiteSpace(clientKeys.PrivateKey) || string.IsNullOrWhiteSpace(clientKeys.PublicKey))
        {
            throw new InvalidOperationException("Generate client keys before creating invitation links.");
        }

        if (string.IsNullOrWhiteSpace(serverKeys.PublicKey))
        {
            throw new InvalidOperationException("Generate server keys before creating invitation links.");
        }

        InvitationEntity invitation = new()
        {
            RemoteUrl = string.Empty,
            ClusterId = clusterId,
            VpnAddress = form.VpnAddress.Trim(),
            RemoteApiKey = GenerateRemoteApiKey(),
            OneTimeVpnKey = GenerateOneTimeVpnKey(),
            InviteLink = string.Empty,
            InviteStatus = "Pending",
            ValidationStatus = "Not claimed"
        };

        invitation.InviteLink = BuildInviteLink(vpnConfig.Endpoint, invitation.OneTimeVpnKey);

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Invitations.Add(invitation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new InvitationListItem(
            invitation.Id,
            invitation.RemoteUrl,
            invitation.ClusterId,
            invitation.VpnAddress,
            invitation.InviteLink,
            invitation.InviteStatus,
            invitation.ValidationStatus,
            invitation.UsedAtUtc,
            invitation.LastSeenAtUtc,
            invitation.CreatedAtUtc);
    }

    public async Task<InviteRemoteServerRequest> GetInviteRequestAsync(string inviteKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inviteKey))
        {
            throw new InvalidOperationException("VPN invite key is required.");
        }

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        InvitationEntity? invitation = await dbContext.Invitations
            .FirstOrDefaultAsync(item => item.OneTimeVpnKey == inviteKey.Trim(), cancellationToken);

        if (invitation is null)
        {
            throw new InvalidOperationException("VPN invite key is invalid.");
        }

        if (invitation.UsedAtUtc is not null)
        {
            throw new InvalidOperationException("VPN invite key has already been used.");
        }

        VpnConfigModel vpnConfig = await vpnConfigService.LoadConfiguredModelAsync(cancellationToken);
        SavedVpnKeyPair clientKeys = await vpnConfigService.LoadClientKeyPairAsync(cancellationToken);
        SavedVpnKeyPair serverKeys = await vpnConfigService.LoadServerKeyPairAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(clientKeys.PrivateKey))
        {
            throw new InvalidOperationException("Client private key is not ready for invite claims.");
        }

        if (string.IsNullOrWhiteSpace(serverKeys.PublicKey))
        {
            throw new InvalidOperationException("Server public key is not ready for invite claims.");
        }

        invitation.UsedAtUtc = DateTimeOffset.UtcNow;
        invitation.InviteStatus = "Claimed";
        invitation.ValidationStatus = "Unknown";
        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildInviteRequest(invitation, vpnConfig, clientKeys, serverKeys);
    }

    private static string GetNextVpnAddress(string? controlAddress, int inviteCount)
    {
        if (string.IsNullOrWhiteSpace(controlAddress))
        {
            throw new InvalidOperationException("VPN address must be configured in wg0.conf before generating invitations.");
        }

        string address = controlAddress.Trim();
        string[] addressParts = address.Split('/', 2, StringSplitOptions.TrimEntries);
        string ipPart = addressParts[0];
        string cidrPart = addressParts.Length > 1 ? addressParts[1] : "32";
        string[] octets = ipPart.Split('.', StringSplitOptions.TrimEntries);

        if (octets.Length == 4 &&
            int.TryParse(octets[0], out int firstOctet) &&
            int.TryParse(octets[1], out int secondOctet) &&
            int.TryParse(octets[2], out int thirdOctet) &&
            int.TryParse(octets[3], out int lastOctet))
        {
            int nextOctet = lastOctet + inviteCount + 1;
            return $"{firstOctet}.{secondOctet}.{thirdOctet}.{nextOctet}/{cidrPart}";
        }

        if (octets.Length >= 3 &&
            int.TryParse(octets[0], out int fallbackFirstOctet) &&
            int.TryParse(octets[1], out int fallbackSecondOctet) &&
            int.TryParse(octets[2], out int fallbackThirdOctet))
        {
            int nextOctet = 3 + inviteCount;
            return $"{fallbackFirstOctet}.{fallbackSecondOctet}.{fallbackThirdOctet}.{nextOctet}/{cidrPart}";
        }

        throw new InvalidOperationException("VPN address format in wg0.conf is invalid for invitation generation.");
    }

    private static string GenerateOneTimeVpnKey()
    {
        return Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant();
    }

    private static string GenerateRemoteApiKey()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }

    private static string BuildInviteLink(string? endpoint, string oneTimeVpnKey)
    {
        string host = string.IsNullOrWhiteSpace(endpoint)
            ? throw new InvalidOperationException("VPN endpoint is required before generating an invite link.")
            : endpoint.Trim();

        if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return $"{host.TrimEnd('/')}/api/vpn/invite/{oneTimeVpnKey}";
        }

        return $"https://{host}/api/vpn/invite/{oneTimeVpnKey}";
    }

    private static InviteRemoteServerRequest BuildInviteRequest(
        InvitationEntity invitation,
        VpnConfigModel vpnConfig,
        SavedVpnKeyPair clientKeys,
        SavedVpnKeyPair serverKeys)
    {
        string endpoint = string.IsNullOrWhiteSpace(vpnConfig.Endpoint)
            ? throw new InvalidOperationException("VPN endpoint is required before creating an invite request.")
            : vpnConfig.Endpoint.Trim();
        string listenPort = string.IsNullOrWhiteSpace(vpnConfig.ListenPort)
            ? throw new InvalidOperationException("VPN listen port is required before creating an invite request.")
            : vpnConfig.ListenPort.Trim();
        string dns = string.IsNullOrWhiteSpace(vpnConfig.Dns)
            ? throw new InvalidOperationException("VPN DNS is required before creating an invite request.")
            : vpnConfig.Dns.Trim();
        string allowedIps = string.IsNullOrWhiteSpace(vpnConfig.AllowedIps)
            ? throw new InvalidOperationException("VPN allowed IPs are required before creating an invite request.")
            : vpnConfig.AllowedIps.Trim();
        string serverPublicKey = string.IsNullOrWhiteSpace(serverKeys.PublicKey)
            ? throw new InvalidOperationException("Server public key is required before creating an invite request.")
            : serverKeys.PublicKey.Trim();
        string clientPrivateKey = string.IsNullOrWhiteSpace(clientKeys.PrivateKey)
            ? throw new InvalidOperationException("Client private key is required before creating an invite request.")
            : clientKeys.PrivateKey.Trim();

        return new InviteRemoteServerRequest(
            invitation.ClusterId,
            invitation.VpnAddress,
            $"{endpoint}:{listenPort}",
            dns,
            allowedIps,
            invitation.RemoteApiKey,
            serverPublicKey,
            clientPrivateKey,
            string.IsNullOrWhiteSpace(vpnConfig.PresharedKey) ? null : vpnConfig.PresharedKey.Trim());
    }

}
