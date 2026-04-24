namespace asa_server_controller.Models.Servers;

public sealed record RemoteInstallAllResponse(
    string ProtonMessage,
    string SteamMessage,
    string StartScriptMessage,
    string ServiceFileMessage);
