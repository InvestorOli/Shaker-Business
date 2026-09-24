using Microsoft.EntityFrameworkCore;

namespace ShakerBusiness.Extensions;

public static class DbContextExtensions
{
    public static async Task<WebApplication> EnsureMigrationsAppliedAsync<TContext>(this WebApplication app)
        where TContext : DbContext
    {
        using var scope = app.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<TContext>>();
        using var context = factory.CreateDbContext();

        var historyTableExists = await TableExistsAsync(context, "__EFMigrationsHistory");
        if (!historyTableExists)
        {
            var firstTable = context.Model.GetEntityTypes().First().GetTableName();
            if (firstTable is not null && await TableExistsAsync(context, firstTable))
            {
                var initialMigrationId = context.Database.GetMigrations().First();
                await context.Database.ExecuteSqlRawAsync(
                    "CREATE TABLE `__EFMigrationsHistory` (`MigrationId` varchar(150) NOT NULL, `ProductVersion` varchar(32) NOT NULL, PRIMARY KEY (`MigrationId`))");
                await context.Database.ExecuteSqlAsync(
                    $"INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) VALUES ({initialMigrationId}, '9.0.9')");
            }
        }

        await context.Database.MigrateAsync();
        return app;
    }

    private static async Task<bool> TableExistsAsync(DbContext context, string tableName)
    {
        return await context.Database.SqlQuery<int>(
            $"SELECT CAST(COUNT(*) AS SIGNED) AS `Value` FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = {tableName}")
            .SingleAsync() > 0;
    }
}
