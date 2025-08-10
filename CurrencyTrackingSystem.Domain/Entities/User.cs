using System.ComponentModel.DataAnnotations;

namespace CurrencyTrackingSystem.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PasswordHash { get; set; } = string.Empty;

        // Навигационное свойство
        public virtual ICollection<UserFavoriteCurrency> FavoriteCurrencies { get; set; } = new HashSet<UserFavoriteCurrency>();
    }
}
