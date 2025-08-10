using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CurrencyTrackingSystem.Domain.Entities
{
    public class Currency
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0.0001, double.MaxValue, ErrorMessage = "Exchange rate must be positive")]
        [Column(TypeName = "decimal(18,6)")]
        public decimal Rate { get; set; }

        // Навигационное свойство
        public virtual ICollection<UserFavoriteCurrency> UserFavorites { get; set; } = new HashSet<UserFavoriteCurrency>();
    }
}
