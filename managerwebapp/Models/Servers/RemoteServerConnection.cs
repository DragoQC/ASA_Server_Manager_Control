namespace managerwebapp.Models.Servers;

public sealed record RemoteServerConnection(
    int Id,
    string RemoteUrl,
    string ApiKey);
