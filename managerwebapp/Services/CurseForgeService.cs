using managerwebapp.Data;
using managerwebapp.Data.Entities;
using managerwebapp.Models.CurseForge;
using Microsoft.EntityFrameworkCore;

namespace managerwebapp.Services;

public sealed class CurseForgeService(
    IDbContextFactory<AppDbContext> dbContextFactory,
    HttpClient httpClient)
{
    private const int SettingsId = 1;

    public async Task<CurseForgeDashboardModel> LoadDashboardAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        CurseForgeSettingsEntity settings = await GetOrCreateSettingsEntityAsync(dbContext, cancellationToken);

        List<CurseForgeCachedModItem> cachedMods = await dbContext.Mods
            .OrderBy(mod => mod.Name)
            .Select(mod => new CurseForgeCachedModItem(
                mod.CurseForgeModId,
                mod.Name,
                mod.Summary,
                mod.WebsiteUrl,
                mod.LogoUrl,
                mod.DownloadCount,
                mod.DateModifiedUtc))
            .ToListAsync(cancellationToken);

        int fleetLinkedModCount = await dbContext.RemoteServerMods
            .Select(link => link.ModEntityId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new CurseForgeDashboardModel(
            new CurseForgeApiSettingsModel
            {
                ApiKey = settings.ApiKey
            },
            !string.IsNullOrWhiteSpace(settings.ApiKey),
            cachedMods.Count,
            fleetLinkedModCount,
            cachedMods);
    }

    public async Task SaveSettingsAsync(CurseForgeApiSettingsModel model, CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        CurseForgeSettingsEntity settings = await GetOrCreateSettingsEntityAsync(dbContext, cancellationToken);
        settings.ApiKey = model.ApiKey?.Trim() ?? string.Empty;
        await dbContext.SaveChangesAsync(cancellationToken);
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
        CurseForgeSettingsEntity settings = await GetOrCreateSettingsEntityAsync(dbContext, cancellationToken);
        HashSet<long> cachedIds = await dbContext.Mods
            .Where(mod => requestedModIds.Contains(mod.CurseForgeModId))
            .Select(mod => mod.CurseForgeModId)
            .ToHashSetAsync(cancellationToken);

        long[] missingIds = requestedModIds.Where(modId => !cachedIds.Contains(modId)).ToArray();
        if (missingIds.Length == 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
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
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        CurseForgeSettingsEntity settings = await GetOrCreateSettingsEntityAsync(dbContext, cancellationToken);
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            return;
        }

        using HttpRequestMessage request = new(HttpMethod.Get, $"/v1/mods/{modId}");
        request.Headers.TryAddWithoutValidation("x-api-key", settings.ApiKey.Trim());

        using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        CurseForgeModApiResponse? payload = await response.Content.ReadFromJsonAsync<CurseForgeModApiResponse>(cancellationToken);
        CurseForgeModData data = payload?.Data ?? throw new InvalidOperationException($"CurseForge mod '{modId}' returned no data.");

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

    private static async Task<CurseForgeSettingsEntity> GetOrCreateSettingsEntityAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        CurseForgeSettingsEntity? settings = await dbContext.CurseForgeSettings
            .FirstOrDefaultAsync(entity => entity.Id == SettingsId, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new CurseForgeSettingsEntity
        {
            Id = SettingsId
        };

        dbContext.CurseForgeSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }
}
