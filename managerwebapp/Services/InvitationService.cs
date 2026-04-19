using System.Net.Http.Json;
using managerwebapp.Data;
using managerwebapp.Data.Entities;
using managerwebapp.Models.Invitations;
using managerwebapp.Models.Vpn;
using Microsoft.EntityFrameworkCore;

namespace managerwebapp.Services;

public sealed class InvitationService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    VpnConfigService vpnConfigService,
    ClusterSettingsService clusterSettingsService,
    RemoteAdminHttpClient remoteAdminHttpClient)
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

    public async Task<InvitationListItem> SendInvitationAsync(InvitationFormModel form, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(form.VpnAddress))
        {
            throw new InvalidOperationException("VPN address is required.");
        }

        bool hasRemoteUrl = !string.IsNullOrWhiteSpace(form.RemoteUrl);
        if (hasRemoteUrl && string.IsNullOrWhiteSpace(form.ApiKey))
        {
            throw new InvalidOperationException("Remote API key is required when a remote URL is provided.");
        }

        string clusterId = await clusterSettingsService.LoadRequiredClusterIdAsync(cancellationToken);
        VpnConfigModel vpnConfig = await vpnConfigService.LoadConfiguredModelAsync(cancellationToken);
        SavedVpnKeyPair clientKeys = await vpnConfigService.LoadClientKeyPairAsync(cancellationToken);
        SavedVpnKeyPair serverKeys = await vpnConfigService.LoadServerKeyPairAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(vpnConfig.Endpoint))
        {
            throw new InvalidOperationException("VPN endpoint is required before sending invitations.");
        }

        if (string.IsNullOrWhiteSpace(vpnConfig.ListenPort))
        {
            throw new InvalidOperationException("VPN listen port is required before sending invitations.");
        }

        if (string.IsNullOrWhiteSpace(vpnConfig.Dns))
        {
            throw new InvalidOperationException("VPN DNS is required before sending invitations.");
        }

        if (string.IsNullOrWhiteSpace(vpnConfig.AllowedIps))
        {
            throw new InvalidOperationException("VPN allowed IPs are required before sending invitations.");
        }

        if (string.IsNullOrWhiteSpace(clientKeys.PrivateKey) || string.IsNullOrWhiteSpace(clientKeys.PublicKey))
        {
            throw new InvalidOperationException("Generate client keys before sending invitations.");
        }

        if (string.IsNullOrWhiteSpace(serverKeys.PublicKey))
        {
            throw new InvalidOperationException("Generate server keys before sending invitations.");
        }

        InvitationEntity invitation = new()
        {
            RemoteUrl = form.RemoteUrl?.Trim() ?? string.Empty,
            ClusterId = clusterId,
            VpnAddress = form.VpnAddress.Trim(),
            OneTimeVpnKey = GenerateOneTimeVpnKey(),
            InviteLink = string.Empty,
            InviteStatus = "Pending",
            ValidationStatus = "Unknown"
        };

        invitation.InviteLink = BuildInviteLink(invitation.OneTimeVpnKey);

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Invitations.Add(invitation);
        await dbContext.SaveChangesAsync(cancellationToken);

        InviteRemoteServerRequest request = BuildInviteRequest(invitation, vpnConfig, clientKeys, serverKeys);

        if (hasRemoteUrl)
        {
            try
            {
                InviteRemoteServerResponse? result = await remoteAdminHttpClient.PostAsJsonAsync<InviteRemoteServerRequest, InviteRemoteServerResponse>(
                    invitation.RemoteUrl,
                    "/api/admin/invite",
                    form.ApiKey!.Trim(),
                    request,
                    cancellationToken);

                invitation.InviteStatus = result?.Accepted == false ? "Failed" : "Accepted";
                invitation.ValidationStatus = "Unknown";
                invitation.UsedAtUtc = result?.Accepted == false ? null : DateTimeOffset.UtcNow;
                await UpsertRemoteServerAsync(dbContext, invitation, form.ApiKey.Trim(), cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                invitation.InviteStatus = "Failed";
                await dbContext.SaveChangesAsync(cancellationToken);
                throw;
            }
        }

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

    public async Task<InviteRemoteServerRequest> ClaimInviteAsync(string inviteKey, CancellationToken cancellationToken = default)
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
        invitation.InviteStatus = "Accepted";
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

    private static string BuildInviteLink(string oneTimeVpnKey)
    {
        return $"/api/vpn/invite/{oneTimeVpnKey}";
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
            serverPublicKey,
            clientPrivateKey,
            string.IsNullOrWhiteSpace(vpnConfig.PresharedKey) ? null : vpnConfig.PresharedKey.Trim());
    }

    private static async Task UpsertRemoteServerAsync(AppDbContext dbContext, InvitationEntity invitation, string apiKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(invitation.RemoteUrl))
        {
            return;
        }

        RemoteServerEntity? remoteServer = await dbContext.RemoteServers
            .FirstOrDefaultAsync(server => server.RemoteUrl == invitation.RemoteUrl, cancellationToken);

        if (remoteServer is null)
        {
            remoteServer = new RemoteServerEntity
            {
                RemoteUrl = invitation.RemoteUrl,
                VpnAddress = invitation.VpnAddress,
                InviteStatus = invitation.InviteStatus,
                ValidationStatus = invitation.ValidationStatus,
                LastSeenAtUtc = invitation.LastSeenAtUtc,
                ApiKey = apiKey
            };

            dbContext.RemoteServers.Add(remoteServer);
            return;
        }

        remoteServer.VpnAddress = invitation.VpnAddress;
        remoteServer.InviteStatus = invitation.InviteStatus;
        remoteServer.ValidationStatus = invitation.ValidationStatus;
        remoteServer.LastSeenAtUtc = invitation.LastSeenAtUtc;
        remoteServer.ApiKey = apiKey;
    }
}
