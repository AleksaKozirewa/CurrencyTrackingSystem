using CurrencyTrackingSystem.Application.DTO.Finance;
using CurrencyTrackingSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTrackingSystem.Application.Interfaces
{
    public interface ICurrencyService
    {
        Task<IEnumerable<CurrencyDto>> GetUserFavoriteCurrenciesAsync(Guid userId);

        Task<IEnumerable<CurrencyDto>> GetAllCurrenciesAsync(Guid? userId = null);

        Task UpdateFavoriteCurrenciesAsync(Guid userId, UpdateFavoriteCurrenciesDto dto);
    }
}
