namespace asa_server_controller.Models.Servers;

public sealed record PatchServerConfigRequest(
    string? ServerName,
    string? MapName,
    int? MaxPlayers,
    int? GamePort,
    IReadOnlyList<string>? ModIds,
    string? ClusterId);
