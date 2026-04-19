namespace managerwebapp.Models.CurseForge;

public sealed record CurseForgeDashboardModel(
    CurseForgeApiSettingsModel Settings,
    bool HasApiKey,
    int CachedModCount,
    int FleetLinkedModCount,
    IReadOnlyList<CurseForgeCachedModItem> CachedMods);
