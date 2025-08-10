using CurrencyTrackingSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTrackingSystem.Infrastructure.Services
{
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<TokenBlacklistService> _logger;
        private readonly IDistributedCache _distributedCache; // Для распределённых систем

        public TokenBlacklistService(
            IMemoryCache cache,
            ILogger<TokenBlacklistService> logger,
            IDistributedCache distributedCache = null)
        {
            _cache = cache;
            _logger = logger;
            _distributedCache = distributedCache;
        }

        public async Task BlacklistTokenAsync(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                {
                    _logger.LogWarning("Invalid JWT token format");
                    return;
                }

                var jwtToken = handler.ReadJwtToken(token);
                var expiry = jwtToken.ValidTo;

                // Для одного сервера
                _cache.Set(token, true, new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = expiry
                });

                // Для распределённых систем (если используется)
                if (_distributedCache != null)
                {
                    await _distributedCache.SetAsync(
                        key: $"blacklisted_token_{token}",
                        value: Encoding.UTF8.GetBytes("1"),
                        options: new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = expiry
                        });
                }

                _logger.LogInformation("Token blacklisted. Expires at: {Expiry}", expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blacklisting token");
                throw;
            }
        }

        public async Task<bool> IsTokenBlacklistedAsync(string token)
        {
            // Проверка в локальном кеше
            if (_cache.TryGetValue(token, out _))
                return true;

            // Проверка в распределённом кеше (если используется)
            if (_distributedCache != null)
            {
                var result = await _distributedCache.GetAsync($"blacklisted_token_{token}");
                if (result != null) return true;
            }

            return false;
        }
    }
}