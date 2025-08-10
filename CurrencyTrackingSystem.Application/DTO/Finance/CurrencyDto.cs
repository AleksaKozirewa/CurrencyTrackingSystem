namespace CurrencyTrackingSystem.Application.DTO.Finance
{
    public class CurrencyDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public decimal Rate { get; set; }
        public bool IsFavorite { get; set; }
    }
}
