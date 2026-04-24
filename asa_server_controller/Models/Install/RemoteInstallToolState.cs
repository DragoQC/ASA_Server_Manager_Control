namespace asa_server_controller.Models.Install;

public sealed record RemoteInstallToolState(
    string Title,
    string Description,
    string Status,
    string StateLabel,
    string? VersionLabel,
    string? LatestVersionLabel,
    string InstallPath,
    bool CanUpdate,
    bool CanRevert);
