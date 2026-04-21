namespace managerwebapp.Models.CurseForge;

public sealed record ModsDashboardModel(
    bool HasApiKey,
    int CachedModCount,
    int FleetLinkedModCount,
    IReadOnlyList<CachedMod> CachedMods);
