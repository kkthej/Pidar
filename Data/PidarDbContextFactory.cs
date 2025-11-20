using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace Pidar.Data
{
    // This factory is ONLY used by EF Core at design time (migrations)
    public class PidarDbContextFactory : IDesignTimeDbContextFactory<PidarDbContext>
    {
        public PidarDbContext CreateDbContext(string[] args)
        {
            var basePath = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables();

            IConfiguration config = builder.Build();

            // Use local DB for Migrations:
            var cs = config.GetConnectionString("DesignTimeConnection");

            var optionsBuilder = new DbContextOptionsBuilder<PidarDbContext>();
            optionsBuilder.UseNpgsql(cs);

            return new PidarDbContext(optionsBuilder.Options);
        }

    }
}
