using managerwebapp.Data;
using managerwebapp.Data.Entities;
using managerwebapp.Models.CurseForge;
using Microsoft.EntityFrameworkCore;

namespace managerwebapp.Services;

public sealed class ModsService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    CurseForgeService curseForgeService)
{
    public async Task<ModsDashboardModel> LoadDashboardAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<CachedMod> cachedMods = await dbContext.Mods
            .OrderBy(mod => mod.Name)
            .Select(mod => new CachedMod(
                mod.CurseForgeModId,
                mod.Name,
                mod.Summary,
                mod.WebsiteUrl,
                mod.LogoUrl,
                dbContext.RemoteServerMods.Any(link => link.ModEntityId == mod.Id),
                mod.DownloadCount,
                mod.DateModifiedUtc))
            .ToListAsync(cancellationToken);

        int fleetLinkedModCount = await dbContext.RemoteServerMods
            .Select(link => link.ModEntityId)
            .Distinct()
            .CountAsync(cancellationToken);

        bool hasApiKey = await curseForgeService.HasApiKeyAsync(cancellationToken);

        return new ModsDashboardModel(
            hasApiKey,
            cachedMods.Count,
            fleetLinkedModCount,
            cachedMods);
    }

    public async Task EnsureModsCachedAsync(IEnumerable<long> modIds, CancellationToken cancellationToken = default)
    {
        long[] requestedModIds = modIds
            .Where(modId => modId > 0)
            .Distinct()
            .ToArray();

        if (requestedModIds.Length == 0)
        {
            return;
        }

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        HashSet<long> cachedIds = await dbContext.Mods
            .Where(mod => requestedModIds.Contains(mod.CurseForgeModId))
            .Select(mod => mod.CurseForgeModId)
            .ToHashSetAsync(cancellationToken);

        long[] missingIds = requestedModIds.Where(modId => !cachedIds.Contains(modId)).ToArray();
        if (missingIds.Length == 0)
        {
            return;
        }

        bool hasApiKey = await curseForgeService.HasApiKeyAsync(cancellationToken);
        if (!hasApiKey)
        {
            foreach (long modId in missingIds)
            {
                dbContext.Mods.Add(new ModEntity
                {
                    CurseForgeModId = modId,
                    Name = $"Mod {modId}",
                    Summary = "Add CurseForge API key to display those informations."
                });
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        foreach (long modId in missingIds)
        {
            await RefreshModAsync(modId, cancellationToken);
        }
    }

    public async Task RefreshAllCachedModsAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        long[] modIds = await dbContext.Mods
            .OrderBy(mod => mod.CurseForgeModId)
            .Select(mod => mod.CurseForgeModId)
            .ToArrayAsync(cancellationToken);

        foreach (long modId in modIds)
        {
            await RefreshModAsync(modId, cancellationToken);
        }
    }

    public async Task RefreshModAsync(long modId, CancellationToken cancellationToken = default)
    {
        CurseForgeModData data = await curseForgeService.GetModAsync(modId, cancellationToken);

        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        ModEntity? entity = await dbContext.Mods
            .FirstOrDefaultAsync(mod => mod.CurseForgeModId == modId, cancellationToken);

        if (entity is null)
        {
            entity = new ModEntity
            {
                CurseForgeModId = modId
            };

            dbContext.Mods.Add(entity);
        }

        entity.Name = string.IsNullOrWhiteSpace(data.Name) ? $"Mod {modId}" : data.Name.Trim();
        entity.Slug = data.Slug?.Trim() ?? string.Empty;
        entity.Summary = data.Summary?.Trim() ?? string.Empty;
        entity.WebsiteUrl = data.Links?.WebsiteUrl?.Trim() ?? string.Empty;
        entity.LogoUrl = data.Logo?.ThumbnailUrl?.Trim()
                         ?? data.Logo?.Url?.Trim()
                         ?? string.Empty;
        entity.DownloadCount = data.DownloadCount;
        entity.DateModifiedUtc = data.DateModified;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
