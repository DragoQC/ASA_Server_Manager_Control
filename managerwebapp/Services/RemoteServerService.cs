using managerwebapp.Data;
using managerwebapp.Models.Servers;
using Microsoft.EntityFrameworkCore;

namespace managerwebapp.Services;

public sealed class RemoteServerService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    public async Task<IReadOnlyList<RemoteServerListItem>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<RemoteServerListItem> items = await dbContext.RemoteServers
            .Select(server => new RemoteServerListItem(
                server.Id,
                server.RemoteUrl,
                server.VpnAddress,
                server.InviteStatus,
                server.ValidationStatus,
                server.LastSeenAtUtc,
                server.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return items
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
                server.RemoteUrl,
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
                server.RemoteUrl,
                server.ApiKey))
            .ToListAsync(cancellationToken);
    }
}
