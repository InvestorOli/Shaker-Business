using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ShakerBusiness.Data;

public class ShakerDbContextFactory : IDesignTimeDbContextFactory<ShakerDbContext>
{
    public ShakerDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets<ShakerDbContext>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("MariaDb");
        var optionsBuilder = new DbContextOptionsBuilder<ShakerDbContext>();
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new ShakerDbContext(optionsBuilder.Options);
    }
}
