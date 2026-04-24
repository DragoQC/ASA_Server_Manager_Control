namespace asa_server_controller.Models.Install;

public sealed record RemoteInstallStateSnapshot(
    int RemoteServerId,
    string ConnectionState,
    RemoteInstallWorkspaceStatusSnapshot Workspace,
    RemoteInstallProgressSnapshot Progress)
{
    public static RemoteInstallStateSnapshot Default(int remoteServerId) =>
        new(
            remoteServerId,
            "Disconnected",
            RemoteInstallWorkspaceStatusSnapshot.Default(),
            RemoteInstallProgressSnapshot.Idle());
}
