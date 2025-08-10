namespace CurrencyTrackingSystem.Application.DTO.User
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid UserId { get; set; }
    }
}
