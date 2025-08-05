using AutoMapper;
using CurrencyTrackingSystem.Application.DTO.Finance;
using CurrencyTrackingSystem.Application.Interfaces;
using CurrencyTrackingSystem.Domain.Entities;
using CurrencyTrackingSystem.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTrackingSystem.Infrastructure.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly ICurrencyRepository _currencyRepository;

        public CurrencyService(ICurrencyRepository currencyRepository)
        {
            _currencyRepository = currencyRepository;
        }

        public async Task<IEnumerable<CurrencyDto>> GetUserFavoriteCurrenciesAsync(Guid userId)
        {
            var currencies = await _currencyRepository.GetUserFavoritesAsync(userId);
            return currencies.Select(MapToDto);
        }

        public async Task<IEnumerable<CurrencyDto>> GetAllCurrenciesAsync(Guid? userId = null)
        {
            var currencies = await _currencyRepository.GetAllAsync();
            var currencyDtos = currencies.Select(MapToDto).ToList();

            if (userId.HasValue)
            {
                var favoriteCurrencies = await _currencyRepository.GetUserFavoritesAsync(userId.Value);
                var favoriteIds = favoriteCurrencies.Select(c => c.Id).ToHashSet();

                foreach (var currency in currencyDtos)
                {
                    currency.IsFavorite = favoriteIds.Contains(currency.Id);
                }
            }

            return currencyDtos;
        }

        public async Task UpdateFavoriteCurrenciesAsync(Guid userId, UpdateFavoriteCurrenciesDto dto)
        {
            // Реализация без изменений, так как не использует маппинг
            var currentFavorites = await _currencyRepository.GetUserFavoritesAsync(userId);
            var currentFavoriteIds = currentFavorites.Select(c => c.Id).ToHashSet();
            var newFavoriteIds = dto.CurrencyIds.ToHashSet();

            foreach (var currencyId in newFavoriteIds.Except(currentFavoriteIds))
            {
                await _currencyRepository.AddToFavoritesAsync(userId, currencyId);
            }

            foreach (var currencyId in currentFavoriteIds.Except(newFavoriteIds))
            {
                await _currencyRepository.RemoveFromFavoritesAsync(userId, currencyId);
            }
        }

        private static CurrencyDto MapToDto(Currency currency)
        {
            return new CurrencyDto
            {
                Id = currency.Id,
                Name = currency.Name,
                Rate = currency.Rate,
                // Другие свойства
                //IsFavorite = currency.UserFavorites // если есть
            };
        }
    }
}
