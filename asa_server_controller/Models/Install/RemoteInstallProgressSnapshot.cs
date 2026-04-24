namespace asa_server_controller.Models.Install;

public sealed record RemoteInstallProgressSnapshot(
    string Operation,
    string Step,
    string State,
    string Message,
    DateTimeOffset UpdatedAtUtc)
{
    public static RemoteInstallProgressSnapshot Idle() =>
        new(
            "idle",
            "idle",
            "Idle",
            "No install operation is running.",
            DateTimeOffset.UtcNow);
}
