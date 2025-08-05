using CurrencyTrackingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTrackingSystem.Domain.Interfaces
{
    public interface ICurrencyRepository
    {
        Task<IEnumerable<Currency>> GetAllAsync();
        Task<IEnumerable<Currency>> GetUserFavoritesAsync(Guid userId);
        Task AddToFavoritesAsync(Guid userId, Guid currencyId);
        Task RemoveFromFavoritesAsync(Guid userId, Guid currencyId);
        Task UpdateRatesAsync(Dictionary<string, decimal> currencyRates);
    }
}
