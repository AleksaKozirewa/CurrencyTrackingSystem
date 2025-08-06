using CurrencyTrackingSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;

namespace CurrencyTrackingSystem.API.Service
{
        public class GatewayTokenBlacklistService : ITokenBlacklistService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<GatewayTokenBlacklistService> _logger;
        private readonly List<string> _tokens = new();
        private readonly object _lock = new();

        public GatewayTokenBlacklistService(
            IMemoryCache cache,
            ILogger<GatewayTokenBlacklistService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public Task BlacklistTokenAsync(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwtToken = handler.ReadJwtToken(token);
                    var expiry = jwtToken.ValidTo;
                    var cacheEntryOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpiration = expiry
                    };

                    lock (_lock)
                    {
                        _cache.Set(token, true, cacheEntryOptions);
                        _tokens.Add(token);
                        _logger.LogInformation("Token blacklisted. Expires at: {Expiry}", expiry);
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid JWT token format");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blacklisting token");
                throw;
            }

            return Task.CompletedTask;
        }

        public Task<bool> IsTokenBlacklistedAsync(string token)
        {
            lock (_lock)
            {
                return Task.FromResult(_cache.TryGetValue(token, out _));
            }
        }

        public Task<int> CleanUpExpiredTokensAsync()
        {
            int removedCount = 0;
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                for (int i = _tokens.Count - 1; i >= 0; i--)
                {
                    var token = _tokens[i];
                    if (!_cache.TryGetValue(token, out _))
                    {
                        _tokens.RemoveAt(i);
                        removedCount++;
                    }
                }
            }
            _logger.LogInformation("Removed {Count} expired tokens", removedCount);
            return Task.FromResult(removedCount);
        }
    }
}
