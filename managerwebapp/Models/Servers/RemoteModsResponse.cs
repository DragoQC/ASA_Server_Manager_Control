using System.Text.Json.Serialization;

namespace managerwebapp.Models.Servers;

public sealed record RemoteModsResponse(
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("modIds")] List<string>? ModIds);
