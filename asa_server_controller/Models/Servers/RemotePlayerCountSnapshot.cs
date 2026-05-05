namespace asa_server_controller.Models.Servers;

public sealed record RemotePlayerCountSnapshot(
    int CurrentPlayers,
    int MaxPlayers,
    string StatusLabel,
    string Message,
    IReadOnlyList<OnlinePlayerSnapshot> Players,
    DateTimeOffset UpdatedAtUtc)
{
    public static RemotePlayerCountSnapshot Default(int maxPlayers = 20) =>
        new(
            CurrentPlayers: 0,
            MaxPlayers: maxPlayers,
            StatusLabel: "Waiting",
            Message: "Waiting for first player poll.",
            Players: [],
            UpdatedAtUtc: DateTimeOffset.UtcNow);
}
