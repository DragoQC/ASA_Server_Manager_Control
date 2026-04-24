using asa_server_controller.Constants;
using asa_server_controller.Models.Servers;

namespace asa_server_controller.Services;

public sealed class RemoteManagerService(
    RemoteAdminHttpClient remoteAdminHttpClient,
    RemoteServerService remoteServerService)
{
    public Task<RemoteManagerCommandResponse> StartAsync(int remoteServerId, CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(remoteServerId, RemoteManagerConstants.StartPath, cancellationToken);
    }

    public Task<RemoteManagerCommandResponse> StopAsync(int remoteServerId, CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(remoteServerId, RemoteManagerConstants.StopPath, cancellationToken);
    }

    public Task<RemoteManagerCommandResponse> RestartAsync(int remoteServerId, CancellationToken cancellationToken = default)
    {
        return SendCommandAsync(remoteServerId, RemoteManagerConstants.RestartPath, cancellationToken);
    }

    public async Task<RemoteManagerCommandResponse> InstallAllAsync(int remoteServerId, CancellationToken cancellationToken = default)
    {
        RemoteServerConnection connection = await remoteServerService.LoadRequiredConnectionAsync(remoteServerId, cancellationToken);
        RemoteInstallAllResponse? response = await remoteAdminHttpClient.PostAsync<RemoteInstallAllResponse>(
            connection.BaseUrl,
            RemoteManagerConstants.InstallAllPath,
            connection.ApiKey,
            cancellationToken);

        if (response is null)
        {
            throw new InvalidOperationException("Remote server returned an empty install response.");
        }

        string message = string.Join(
            " ",
            new[]
            {
                response.ProtonMessage,
                response.SteamMessage,
                response.StartScriptMessage,
                response.ServiceFileMessage
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return new RemoteManagerCommandResponse(true, message, "Installed");
    }

    private async Task<RemoteManagerCommandResponse> SendCommandAsync(int remoteServerId, string relativePath, CancellationToken cancellationToken)
    {
        RemoteServerConnection connection = await remoteServerService.LoadRequiredConnectionAsync(remoteServerId, cancellationToken);
        RemoteManagerCommandResponse? response = await remoteAdminHttpClient.PostAsync<RemoteManagerCommandResponse>(
            connection.BaseUrl,
            relativePath,
            connection.ApiKey,
            cancellationToken);

        if (response is null)
        {
            throw new InvalidOperationException("Remote server returned an empty manager response.");
        }

        return response;
    }
}
