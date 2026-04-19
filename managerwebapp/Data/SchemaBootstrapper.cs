using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace managerwebapp.Data;

public static class SchemaBootstrapper
{
    public static async Task EnsureRemoteServersColumnsAsync(AppDbContext dbContext, CancellationToken cancellationToken = default)
    {
        HashSet<string> columns = await GetColumnsAsync(dbContext, "RemoteServers", cancellationToken);

        if (!columns.Contains("RemoteUrl"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "RemoteServers" ADD COLUMN "RemoteUrl" TEXT NOT NULL DEFAULT '';""",
                cancellationToken);
        }

        if (!columns.Contains("InviteStatus"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "RemoteServers" ADD COLUMN "InviteStatus" TEXT NOT NULL DEFAULT 'Unknown';""",
                cancellationToken);
        }

        if (!columns.Contains("ValidationStatus"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "RemoteServers" ADD COLUMN "ValidationStatus" TEXT NOT NULL DEFAULT 'Unknown';""",
                cancellationToken);
        }

        if (!columns.Contains("LastSeenAtUtc"))
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "RemoteServers" ADD COLUMN "LastSeenAtUtc" TEXT NULL;""",
                cancellationToken);
        }
    }

    private static async Task<HashSet<string>> GetColumnsAsync(AppDbContext dbContext, string tableName, CancellationToken cancellationToken)
    {
        HashSet<string> columns = new(StringComparer.OrdinalIgnoreCase);
        DbConnection connection = dbContext.Database.GetDbConnection();

        bool shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using DbCommand command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

            await using DbDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                columns.Add(reader.GetString(1));
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        return columns;
    }
}
