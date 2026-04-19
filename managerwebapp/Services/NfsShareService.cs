using managerwebapp.Constants;
using managerwebapp.Data;
using managerwebapp.Data.Entities;
using managerwebapp.Models.Cluster;
using managerwebapp.Models.Vpn;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace managerwebapp.Services;

public sealed class NfsShareService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    NfsConfigurationService nfsConfigurationService,
    VpnConfigService vpnConfigService)
{
    public async Task<IReadOnlyList<NfsShareInviteServerOption>> LoadTargetServersAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.RemoteServers
            .Where(server => server.InviteStatus == "Accepted")
            .OrderBy(server => server.VpnAddress)
            .Select(server => new NfsShareInviteServerOption(
                server.Id,
                server.VpnAddress,
                server.Port))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NfsShareInviteListItem>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<NfsShareInviteListItem> items = await dbContext.NfsShareInvites
            .Select(invite => new NfsShareInviteListItem(
                invite.Id,
                invite.RemoteServerId,
                invite.RemoteServer.VpnAddress,
                invite.InviteLink,
                invite.UsedAtUtc,
                invite.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return items
            .OrderByDescending(invite => invite.CreatedAtUtc)
            .ToList();
    }

    public async Task<NfsShareInviteResponse> BuildPreviewAsync(int remoteServerId, CancellationToken cancellationToken = default)
    {
        if (remoteServerId <= 0)
        {
            throw new InvalidOperationException("Remote server is required.");
        }

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        bool exists = await dbContext.RemoteServers
            .AnyAsync(server => server.Id == remoteServerId && server.InviteStatus == "Accepted", cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Accepted remote server is required for an NFS invite.");
        }

        NfsConfigurationModel configuration = await nfsConfigurationService.LoadAsync(cancellationToken);
        return new NfsShareInviteResponse(
            ClusterShareConstants.ClusterDirectoryPath,
            ClusterShareConstants.ClientMountPath,
            configuration.ClientConfigContent);
    }

    public async Task<string> CreateInviteLinkAsync(int remoteServerId, CancellationToken cancellationToken = default)
    {
        if (remoteServerId <= 0)
        {
            throw new InvalidOperationException("Remote server is required.");
        }

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        RemoteServerEntity? remoteServer = await dbContext.RemoteServers
            .FirstOrDefaultAsync(server => server.Id == remoteServerId && server.InviteStatus == "Accepted", cancellationToken);

        if (remoteServer is null)
        {
            throw new InvalidOperationException("Accepted remote server was not found.");
        }

        VpnConfigModel vpnConfig = await vpnConfigService.LoadConfiguredModelAsync(cancellationToken);
        string inviteKey = GenerateInviteKey();
        string inviteLink = BuildInviteLink(vpnConfig.Endpoint, vpnConfig.ListenPort, inviteKey);

        dbContext.NfsShareInvites.Add(new NfsShareInviteEntity
        {
            RemoteServerId = remoteServer.Id,
            RemoteServer = remoteServer,
            InviteKey = inviteKey,
            InviteLink = inviteLink
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return inviteLink;
    }

    public async Task<NfsShareInviteResponse> GetShareRequestAsync(string inviteKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inviteKey))
        {
            throw new InvalidOperationException("NFS invite key is required.");
        }

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        NfsShareInviteEntity? invite = await dbContext.NfsShareInvites
            .Include(item => item.RemoteServer)
            .FirstOrDefaultAsync(item => item.InviteKey == inviteKey.Trim(), cancellationToken);

        if (invite is null)
        {
            throw new InvalidOperationException("NFS invite key is invalid.");
        }

        if (invite.UsedAtUtc is not null)
        {
            throw new InvalidOperationException("NFS invite key has already been used.");
        }

        invite.UsedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        NfsConfigurationModel configuration = await nfsConfigurationService.LoadAsync(cancellationToken);
        return new NfsShareInviteResponse(
            ClusterShareConstants.ClusterDirectoryPath,
            ClusterShareConstants.ClientMountPath,
            configuration.ClientConfigContent);
    }

    private static string GenerateInviteKey()
    {
        return Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
    }

    private static string BuildInviteLink(string? endpoint, string? listenPort, string inviteKey)
    {
        string host = string.IsNullOrWhiteSpace(endpoint)
            ? throw new InvalidOperationException("VPN endpoint is required before generating an NFS invite link.")
            : NormalizeInviteEndpoint(endpoint.Trim(), listenPort);

        if (host.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            host.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return $"{host.TrimEnd('/')}/api/nfs/invite/{inviteKey}";
        }

        return $"https://{host}/api/nfs/invite/{inviteKey}";
    }

    private static string NormalizeInviteEndpoint(string endpoint, string? listenPort)
    {
        if (string.IsNullOrWhiteSpace(listenPort))
        {
            return endpoint;
        }

        string suffix = $":{listenPort.Trim()}";
        if (!endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
            endpoint.EndsWith(suffix, StringComparison.Ordinal))
        {
            return endpoint[..^suffix.Length];
        }

        return endpoint;
    }
}
