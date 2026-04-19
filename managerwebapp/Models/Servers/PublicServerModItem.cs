namespace managerwebapp.Models.Servers;

public sealed record PublicServerModItem(
    long ModId,
    string Name,
    string Summary,
    string WebsiteUrl,
    string LogoUrl);
