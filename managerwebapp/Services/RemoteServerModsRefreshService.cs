using System.Collections.Concurrent;
using managerwebapp.Models.Servers;

namespace managerwebapp.Services;

public sealed class RemoteServerModsRefreshService(
    RemoteServerHubClientService remoteServerHubClientService,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<RemoteServerModsRefreshService> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<int, DateTimeOffset> _lastSyncByServerId = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        remoteServerHubClientService.StatusUpdated += OnStatusUpdatedAsync;

        try
        {
            await SyncAcceptedServersAsync(stoppingToken);

            using PeriodicTimer timer = new(TimeSpan.FromMinutes(10));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await SyncAcceptedServersAsync(stoppingToken);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to refresh remote server mods.");
                }
            }
        }
        finally
        {
            remoteServerHubClientService.StatusUpdated -= OnStatusUpdatedAsync;
        }
    }

    private Task OnStatusUpdatedAsync(int remoteServerId, RemoteAsaServiceStatus status)
    {
        if (!status.IsUpOrStarting)
        {
            return Task.CompletedTask;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (_lastSyncByServerId.TryGetValue(remoteServerId, out DateTimeOffset lastSync) &&
            now - lastSync < TimeSpan.FromMinutes(1))
        {
            return Task.CompletedTask;
        }

        _lastSyncByServerId[remoteServerId] = now;
        _ = SyncInBackgroundAsync(remoteServerId);
        return Task.CompletedTask;
    }

    private async Task SyncInBackgroundAsync(int remoteServerId)
    {
        try
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            RemoteServerModsService remoteServerModsService = scope.ServiceProvider.GetRequiredService<RemoteServerModsService>();
            await remoteServerModsService.SyncRemoteServerAsync(remoteServerId);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to sync mods for remote server {RemoteServerId}.", remoteServerId);
        }
    }

    private async Task SyncAcceptedServersAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceScopeFactory.CreateScope();
        RemoteServerModsService remoteServerModsService = scope.ServiceProvider.GetRequiredService<RemoteServerModsService>();
        await remoteServerModsService.SyncAcceptedServersAsync(cancellationToken);
    }
}
