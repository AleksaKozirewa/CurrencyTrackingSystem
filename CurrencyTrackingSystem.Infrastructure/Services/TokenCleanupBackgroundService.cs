using CurrencyTrackingSystem.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTrackingSystem.Infrastructure.Services
{
    public class TokenCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<TokenCleanupBackgroundService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

        public TokenCleanupBackgroundService(
            IServiceProvider services,
            ILogger<TokenCleanupBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token cleanup service is starting");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var tokenBlacklistService = scope.ServiceProvider
                        .GetRequiredService<ITokenBlacklistService>();

                    var removedCount = await tokenBlacklistService.CleanUpExpiredTokensAsync();
                    _logger.LogDebug("Cleaned up {Count} expired tokens", removedCount);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up expired tokens");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }

            _logger.LogInformation("Token cleanup service is stopping");
        }
    }
}
