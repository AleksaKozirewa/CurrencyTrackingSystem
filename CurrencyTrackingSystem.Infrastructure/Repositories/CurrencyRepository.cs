using CurrencyTrackingSystem.Domain.Entities;
using CurrencyTrackingSystem.Domain.Interfaces;
using CurrencyTrackingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CurrencyTrackingSystem.Infrastructure.Repositories
{
    public class CurrencyRepository : ICurrencyRepository
    {
        private readonly AppDbContext _context;

        public CurrencyRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Currency>> GetUserFavoritesAsync(Guid userId)
        {
            return await _context.UserFavoriteCurrencies
                .Where(ufc => ufc.UserId == userId)
                .Include(ufc => ufc.Currency)
                .Select(ufc => ufc.Currency)
                .ToListAsync();
        }

        public async Task AddToFavoritesAsync(Guid userId, Guid currencyId)
        {
            var existing = await _context.UserFavoriteCurrencies
                .FirstOrDefaultAsync(ufc => ufc.UserId == userId && ufc.CurrencyId == currencyId);

            if (existing == null)
            {
                _context.UserFavoriteCurrencies.Add(new UserFavoriteCurrency
                {
                    UserId = userId,
                    CurrencyId = currencyId
                });
                await _context.SaveChangesAsync();
            }
        }

        public async Task RemoveFromFavoritesAsync(Guid userId, Guid currencyId)
        {
            var favorite = await _context.UserFavoriteCurrencies
                .FirstOrDefaultAsync(ufc => ufc.UserId == userId && ufc.CurrencyId == currencyId);

            if (favorite != null)
            {
                _context.UserFavoriteCurrencies.Remove(favorite);
                await _context.SaveChangesAsync();
            }
        }
    }
}
