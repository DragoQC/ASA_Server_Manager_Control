using Microsoft.AspNetCore.SignalR.Client;

namespace managerwebapp.Models.Servers;

public sealed record RemoteHubRegistration(
    HubConnection Connection,
    string BaseUrl,
    string ApiKey);
