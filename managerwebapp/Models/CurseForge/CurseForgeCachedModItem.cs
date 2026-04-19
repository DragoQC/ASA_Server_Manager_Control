namespace managerwebapp.Models.CurseForge;

public sealed record CurseForgeCachedModItem(
    long ModId,
    string Name,
    string Summary,
    string WebsiteUrl,
    string LogoUrl,
    long DownloadCount,
    DateTimeOffset? DateModifiedUtc);
