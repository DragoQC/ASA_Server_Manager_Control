using managerwebapp.Data;
using managerwebapp.Data.Entities;
using managerwebapp.Models.Servers;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;

namespace managerwebapp.Services;

public sealed class RemoteServerService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    RemoteServerHubClientService remoteServerHubClientService)
{
    public const string DefaultRemoteServerPort = "8000";

    public async Task<IReadOnlyList<RemoteServerListItem>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<RemoteServerListItem> items = await dbContext.RemoteServers
            .Select(server => new RemoteServerListItem(
                server.Id,
                server.VpnAddress,
                server.Port,
                server.ValidationStatus,
                false,
                false,
                false,
                server.LastSeenAtUtc,
                server.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<RemoteServerListItem> updatedItems = [];
        IReadOnlyDictionary<int, RemoteServerHubSnapshot> snapshots = remoteServerHubClientService.GetSnapshots();

        foreach (RemoteServerListItem item in items)
        {
            bool isReachable = await PingAddressAsync(GetIpAddress(item.VpnAddress));
            if (!isReachable)
            {
                updatedItems.Add(item with
                {
                    StateLabel = "Unknown",
                    CanStart = false,
                    CanStop = false,
                    CanOpenRcon = false
                });
                continue;
            }

            if (!snapshots.TryGetValue(item.Id, out RemoteServerHubSnapshot? snapshot) ||
                !string.Equals(snapshot.ConnectionState, "Connected", StringComparison.Ordinal))
            {
                updatedItems.Add(item with
                {
                    StateLabel = "Reachable",
                    CanStart = false,
                    CanStop = false,
                    CanOpenRcon = false,
                    LastSeenAtUtc = now
                });
                continue;
            }

            updatedItems.Add(item with
            {
                StateLabel = string.IsNullOrWhiteSpace(snapshot.AsaStatus.DisplayText) ? "Reachable" : snapshot.AsaStatus.DisplayText,
                CanStart = snapshot.AsaStatus.CanStart,
                CanStop = snapshot.AsaStatus.CanStop,
                CanOpenRcon = snapshot.AsaStatus.IsRunning,
                LastSeenAtUtc = snapshot.UpdatedAtUtc
            });
        }

        return updatedItems
            .OrderByDescending(server => server.CreatedAtUtc)
            .ToList();
    }

    public async Task<RemoteServerConnection?> LoadConnectionAsync(int remoteServerId, CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.RemoteServers
            .Where(server => server.Id == remoteServerId)
            .Select(server => new RemoteServerConnection(
                server.Id,
                server.VpnAddress,
                server.Port,
                server.ApiKey))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RemoteServerConnection> LoadRequiredConnectionAsync(int remoteServerId, CancellationToken cancellationToken = default)
    {
        RemoteServerConnection? connection = await LoadConnectionAsync(remoteServerId, cancellationToken);
        return connection ?? throw new InvalidOperationException($"Remote server '{remoteServerId}' was not found.");
    }

    public async Task<IReadOnlyList<RemoteServerConnection>> LoadConnectionsAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.RemoteServers
            .OrderBy(server => server.Id)
            .Select(server => new RemoteServerConnection(
                server.Id,
                server.VpnAddress,
                server.Port,
                server.ApiKey))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClaimedInvitationOption>> LoadClaimedInvitationOptionsAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        string[] registeredVpnAddresses = await dbContext.RemoteServers
            .Select(server => server.VpnAddress)
            .ToArrayAsync(cancellationToken);

        return await dbContext.Invitations
            .Where(invitation => invitation.InviteStatus == "Accepted" && !registeredVpnAddresses.Contains(invitation.VpnAddress))
            .OrderBy(invitation => invitation.VpnAddress)
            .Select(invitation => new ClaimedInvitationOption(
                invitation.Id,
                invitation.VpnAddress,
                invitation.RemoteApiKey,
                invitation.UsedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task AddFromInvitationAsync(int invitationId, string port, CancellationToken cancellationToken = default)
    {
        if (invitationId <= 0)
        {
            throw new InvalidOperationException("Claimed invitation is required.");
        }

        string normalizedPort = string.IsNullOrWhiteSpace(port)
            ? throw new InvalidOperationException("Port is required.")
            : port.Trim();
        if (!int.TryParse(normalizedPort, out int parsedPort) || parsedPort <= 0)
        {
            throw new InvalidOperationException("Port is invalid.");
        }

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        InvitationEntity invitation = await dbContext.Invitations
            .FirstOrDefaultAsync(item => item.Id == invitationId, cancellationToken)
            ?? throw new InvalidOperationException("Claimed invitation was not found.");

        if (!string.Equals(invitation.InviteStatus, "Accepted", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invitation must be claimed before adding the server.");
        }

        bool exists = await dbContext.RemoteServers.AnyAsync(
            server => server.VpnAddress == invitation.VpnAddress,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Server is already registered.");
        }

        dbContext.RemoteServers.Add(new RemoteServerEntity
        {
            VpnAddress = invitation.VpnAddress,
            Port = parsedPort,
            InviteStatus = "Accepted",
            ValidationStatus = invitation.ValidationStatus,
            LastSeenAtUtc = invitation.LastSeenAtUtc,
            ApiKey = invitation.RemoteApiKey
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string GetIpAddress(string address)
    {
        return address.Split('/', 2, StringSplitOptions.TrimEntries)[0];
    }

    private static async Task<bool> PingAddressAsync(string ipAddress)
    {
        try
        {
            using Ping ping = new();
            PingReply reply = await ping.SendPingAsync(ipAddress, 1500);
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }
}
