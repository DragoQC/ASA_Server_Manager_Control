using managerwebapp.Data;
using managerwebapp.Data.Entities;
using managerwebapp.Models.Vpn;
using Microsoft.EntityFrameworkCore;

namespace managerwebapp.Services;

public sealed class VpnServerSettingsService(IDbContextFactory<AppDbContext> dbContextFactory)
{
    private const int SettingsId = 1;

    public async Task<VpnServerSettingsModel> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        VpnServerSettingsEntity settings = await GetOrCreateSettingsEntityAsync(dbContext, cancellationToken);

        return new VpnServerSettingsModel
        {
            Endpoint = string.IsNullOrWhiteSpace(settings.Endpoint) ? null : settings.Endpoint,
            AllowedIps = string.IsNullOrWhiteSpace(settings.AllowedIps) ? VpnConfigService.DefaultAllowedIps : settings.AllowedIps,
            PersistentKeepalive = string.IsNullOrWhiteSpace(settings.PersistentKeepalive) ? VpnConfigService.DefaultPersistentKeepalive : settings.PersistentKeepalive,
            PresharedKey = string.IsNullOrWhiteSpace(settings.PresharedKey) ? null : settings.PresharedKey
        };
    }

    public async Task SaveAsync(VpnServerSettingsModel model, CancellationToken cancellationToken = default)
    {
        await using AppDbContext dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        VpnServerSettingsEntity settings = await GetOrCreateSettingsEntityAsync(dbContext, cancellationToken);
        settings.Endpoint = model.Endpoint?.Trim() ?? string.Empty;
        settings.AllowedIps = model.AllowedIps?.Trim() ?? string.Empty;
        settings.PersistentKeepalive = model.PersistentKeepalive?.Trim() ?? string.Empty;
        settings.PresharedKey = model.PresharedKey?.Trim() ?? string.Empty;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<VpnServerSettingsEntity> GetOrCreateSettingsEntityAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        VpnServerSettingsEntity? settings = await dbContext.VpnServerSettings
            .FirstOrDefaultAsync(entity => entity.Id == SettingsId, cancellationToken);

        if (settings is not null)
        {
            return settings;
        }

        settings = new VpnServerSettingsEntity
        {
            Id = SettingsId
        };

        dbContext.VpnServerSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }
}
