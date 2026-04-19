namespace managerwebapp.Models.Servers;

public sealed record RemoteServerHubSnapshot(
    int RemoteServerId,
    string ConnectionState,
    RemoteAsaServiceStatus AsaStatus,
    RemotePlayerCountSnapshot PlayerCount,
    DateTimeOffset UpdatedAtUtc)
{
    public static RemoteServerHubSnapshot Default(int remoteServerId)
    {
        return new RemoteServerHubSnapshot(
            remoteServerId,
            "Disconnected",
            RemoteAsaServiceStatus.Unknown("Disconnected"),
            RemotePlayerCountSnapshot.Default(),
            DateTimeOffset.UtcNow);
    }
}
