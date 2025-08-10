using CurrencyTrackingSystem.Domain.Entities;

namespace CurrencyTrackingSystem.Domain.Interfaces
{
    public interface ICurrencyRepository
    {
        Task<IEnumerable<Currency>> GetUserFavoritesAsync(Guid userId);
        Task AddToFavoritesAsync(Guid userId, Guid currencyId);
        Task RemoveFromFavoritesAsync(Guid userId, Guid currencyId);
    }
}
