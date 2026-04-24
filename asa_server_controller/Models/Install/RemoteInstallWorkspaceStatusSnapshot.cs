namespace asa_server_controller.Models.Install;

public sealed record RemoteInstallWorkspaceStatusSnapshot(
    RemoteInstallToolState Proton,
    RemoteInstallToolState Steam,
    RemoteInstallFileStatusSnapshot StartScript,
    RemoteInstallFileStatusSnapshot ServiceFile,
    DateTimeOffset CheckedAtUtc)
{
    public static RemoteInstallWorkspaceStatusSnapshot Default() =>
        new(
            new RemoteInstallToolState("Proton", string.Empty, "Missing", "Missing", null, null, string.Empty, false, false),
            new RemoteInstallToolState("Steam", string.Empty, "Missing", "Missing", null, null, string.Empty, false, false),
            new RemoteInstallFileStatusSnapshot("Start script", string.Empty, "Missing", "Missing", string.Empty),
            new RemoteInstallFileStatusSnapshot("Service file", string.Empty, "Missing", "Missing", string.Empty),
            DateTimeOffset.UtcNow);
}
