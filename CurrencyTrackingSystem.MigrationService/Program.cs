using CurrencyTrackingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

//namespace CurrencyTrackingSystem.MigrationService
//{
//    public class Program
//    {
//        public static async Task Main(string[] args)
//        {
//            var host = Host.CreateDefaultBuilder(args)
//                .ConfigureAppConfiguration(config =>
//                {
//                    config.SetBasePath(Directory.GetCurrentDirectory());
//                    config.AddJsonFile("appsettings.json", optional: false);
//                })
//                .ConfigureServices((hostContext, services) =>
//                {
//                    services.AddDbContext<AppDbContext>(options =>
//                        options.UseNpgsql(
//                            hostContext.Configuration.GetConnectionString("DefaultConnection"),
//                            npgsql => npgsql.MigrationsAssembly("CurrencyTrackingSystem.MigrationService")));
//                })
//                .Build();

//            using var scope = host.Services.CreateScope();
//            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//            try
//            {
//                Console.WriteLine("Applying database migrations...");
//                await dbContext.Database.MigrateAsync();
//                Console.WriteLine("Migrations applied successfully!");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"Migration failed: {ex.Message}");
//                throw;
//            }
//        }
//    }
//}

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

//namespace CurrencyTrackingSystem.MigrationService
//{
//    public class Program
//    {
//        //public static async Task Main(string[] args)
//        //{      

//        //try
//        //{
//        //    Console.Title = "Database Migration Service";
//        //    Console.WriteLine("Starting migration process...");

//        //    var host = BuildHost(args);
//        //    await RunMigrationsAsync(host);

//        //    Console.WriteLine("Migration process completed successfully");
//        //    Environment.Exit(0); // Успешное завершение
//        //}
//        //catch (Exception ex)
//        //{
//        //    Console.ForegroundColor = ConsoleColor.Red;
//        //    Console.WriteLine($"Critical migration error: {ex.Message}");
//        //    Console.ResetColor();
//        //    Environment.Exit(1); // Завершение с ошибкой
//        //}
//    }
//}
//        private static IHost BuildHost(string[] args)
//        {
//            return Host.CreateDefaultBuilder(args)
//                .ConfigureAppConfiguration((hostContext, config) =>
//                {
//                    config.SetBasePath(Directory.GetCurrentDirectory());
//                    config.AddJsonFile("appsettings.json", optional: false);
//                    //config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
//                    config.AddEnvironmentVariables();
//                })
//                .ConfigureServices((hostContext, services) =>
//                {
//                    services.AddDbContext<AppDbContext>(options =>
//                    {
//                        var connectionString = hostContext.Configuration.GetConnectionString("DefaultConnection");
//                        if (string.IsNullOrEmpty(connectionString))
//                        {
//                            throw new InvalidOperationException("Database connection string is not configured");
//                        }

//                        options.UseNpgsql(connectionString, npgsql =>
//                        {
//                            npgsql.MigrationsAssembly("CurrencyTrackingSystem.MigrationService");
//                            npgsql.EnableRetryOnFailure(
//                                maxRetryCount: 5,
//                                maxRetryDelay: TimeSpan.FromSeconds(30),
//                                errorCodesToAdd: null);
//                            npgsql.CommandTimeout(300); // 5 минут для сложных миграций
//                        });

//                        if (hostContext.HostingEnvironment.IsDevelopment())
//                        {
//                            options.EnableDetailedErrors();
//                            options.EnableSensitiveDataLogging();
//                        }
//                    });
//                })
//                .UseConsoleLifetime()
//                .Build();
//        }

//        private static async Task RunMigrationsAsync(IHost host)
//        {
//            using var scope = host.Services.CreateScope();
//            var services = scope.ServiceProvider;
//            var logger = services.GetRequiredService<ILogger<Program>>();
//            var dbContext = services.GetRequiredService<AppDbContext>();

//            try
//            {
//                logger.LogInformation("Checking database connection...");
//                if (!await dbContext.Database.CanConnectAsync())
//                {
//                    throw new Exception("Could not connect to the database");
//                }

//                logger.LogInformation("Applying pending migrations...");
//                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
//                if (pendingMigrations.Any())
//                {
//                    logger.LogInformation($"Pending migrations: {string.Join(", ", pendingMigrations)}");
//                    await dbContext.Database.MigrateAsync();
//                    logger.LogInformation("Migrations applied successfully");
//                }
//                else
//                {
//                    logger.LogInformation("No pending migrations found");
//                }

//                logger.LogInformation("Verifying applied migrations...");
//                var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
//                logger.LogInformation($"Current database version: {appliedMigrations.LastOrDefault() ?? "None"}");
//            }
//            catch (Npgsql.PostgresException pgEx) when (pgEx.SqlState == "42501")
//            {
//                logger.LogError("Permission denied error. Possible solutions:");
//                logger.LogError("1. Grant required permissions to your database user:");
//                logger.LogError("   GRANT ALL PRIVILEGES ON SCHEMA public TO your_user;");
//                logger.LogError("   GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO your_user;");
//                logger.LogError("2. Or use a database user with higher privileges for migrations");
//                throw;
//            }
//            catch (Exception ex)
//            {
//                logger.LogError(ex, "Migration failed");
//                throw;
//            }
//        }

//        //var builder = Host.CreateDefaultBuilder(args)
//        //    .ConfigureServices((hostContext, services) =>
//        //    {
//        //        services.AddDbContext<AppDbContext>(options =>
//        //            options.UseNpgsql(
//        //                hostContext.Configuration.GetConnectionString("DefaultConnection"),
//        //                npgsql => npgsql.MigrationsAssembly(typeof(Program).Assembly.FullName)));
//        //    })
//        //    .UseConsoleLifetime(); // Добавьте это

//        //var host = builder.Build();

//        //using (var scope = host.Services.CreateScope())
//        //{
//        //    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//        //    await db.Database.MigrateAsync();
//        //}

//        //await host.RunAsync(); // Критически важно для .NET 9+

//}