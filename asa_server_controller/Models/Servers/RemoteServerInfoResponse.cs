using System.Text.Json.Serialization;

namespace asa_server_controller.Models.Servers;

public sealed record RemoteServerInfoResponse(
    bool Success,
    string ServerName,
    string MapName,
    int MaxPlayers,
    int? GamePort,
    DateTimeOffset CheckedAtUtc,
    [property: JsonPropertyName("modIds")] List<string>? ModIds,
    string? TotalRam = null,
    string? RamPercentage = null,
    string? CpuUsage = null,
    string? DiskTotal = null,
    string? DiskUsed = null);
