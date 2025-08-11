using CurrencyTrackingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CurrencyTrackingSystem.MigrationService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddDbContext<AppDbContext>(options =>
                        options.UseNpgsql(
                            hostContext.Configuration.GetConnectionString("DefaultConnection"),
                            npgsql => npgsql.MigrationsAssembly(typeof(Program).Assembly.FullName)));
                })
                .Build();

            using var scope = host.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Testing database connection...");
                if (!await dbContext.Database.CanConnectAsync())
                {
                    throw new Exception("Could not connect to database");
                }

                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
                if (!pendingMigrations.Any())
                {
                    logger.LogInformation("No pending migrations.");
                    return;
                }

                logger.LogInformation("Applying {Count} migrations...", pendingMigrations.Count());
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Migrations applied successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Migration failed");
                throw;
            }
        }
    }
}