namespace managerwebapp.Models.Servers;

public sealed record RemoteManagerCommandResponse(
    bool Success,
    string Message,
    string State);
