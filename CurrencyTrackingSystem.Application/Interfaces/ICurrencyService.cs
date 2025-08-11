using CurrencyTrackingSystem.Application.DTO.Finance;

namespace CurrencyTrackingSystem.Application.Interfaces
{
    public interface ICurrencyService
    {
        Task<IEnumerable<CurrencyDto>> GetUserFavoriteCurrenciesAsync(Guid userId);
        Task UpdateFavoriteCurrenciesAsync(Guid userId, UpdateFavoriteCurrenciesDto dto);
    }
}
