using KickOffEventVoting.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace KickOffEvent.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            var connStr = configuration.GetConnectionString("DefaultConnection");

            optionsBuilder.UseSqlServer(connStr, sql => sql.EnableRetryOnFailure());

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
