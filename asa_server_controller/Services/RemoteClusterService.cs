using asa_server_controller.Constants;
using asa_server_controller.Models.Servers;

namespace asa_server_controller.Services;

public sealed class RemoteClusterService(
    RemoteAdminHttpClient remoteAdminHttpClient,
    RemoteServerService remoteServerService,
    RemoteServerHubClientService remoteServerHubClientService,
    ILogger<RemoteClusterService> logger)
{
    public async Task<int> PushClusterIdToConnectedServersAsync(string clusterId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clusterId))
        {
            return 0;
        }

        IReadOnlyList<RemoteServerConnection> connections = await remoteServerService.LoadConnectionsAsync(cancellationToken);
        int updatedCount = 0;

        foreach (RemoteServerConnection connection in connections)
        {
            if (!string.Equals(remoteServerHubClientService.GetSnapshot(connection.Id).ConnectionState, "Connected", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                await remoteAdminHttpClient.PatchAsJsonAsync<PatchServerConfigRequest, object>(
                    connection.BaseUrl,
                    ClusterConstants.RemoteServerConfigPath,
                    connection.ApiKey,
                    new PatchServerConfigRequest(
                        null,
                        null,
                        null,
                        null,
                        null,
                        clusterId.Trim(),
                        null),
                    cancellationToken);

                updatedCount++;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to push cluster ID to remote server {RemoteServerId}.", connection.Id);
            }
        }

        return updatedCount;
    }
}
