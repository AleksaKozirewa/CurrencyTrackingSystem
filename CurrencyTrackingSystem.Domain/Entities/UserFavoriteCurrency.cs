using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace CurrencyTrackingSystem.Domain.Entities
{
    public class UserFavoriteCurrency
    {
        [Required]
        [ForeignKey("User")] // Указываем связь с пользователем
        public Guid UserId { get; set; }

        [Required]
        [ForeignKey("Currency")] // Указываем связь с валютой
        public Guid CurrencyId { get; set; }

        // Навигационные свойства
        public virtual User User { get; set; } = null!;
        public virtual Currency Currency { get; set; } = null!;
    }
}
