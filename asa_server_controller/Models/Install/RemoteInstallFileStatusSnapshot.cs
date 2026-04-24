namespace asa_server_controller.Models.Install;

public sealed record RemoteInstallFileStatusSnapshot(
    string Title,
    string Description,
    string Status,
    string StateLabel,
    string FilePath);
