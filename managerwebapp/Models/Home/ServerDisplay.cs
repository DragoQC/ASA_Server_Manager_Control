namespace managerwebapp.Models.Home;

public sealed record ServerDisplay(
    IReadOnlyList<HomeServerModel> Servers);
