using CurrencyTrackingSystem.Application.DTO.User;

namespace CurrencyTrackingSystem.Application.Interfaces
{
    public interface IUserService
    {
        Task<AuthResult> RegisterUserAsync(UserRegistrationDto dto);
        Task<string> LoginAsync(UserLoginDto dto);
        Task IsInvalidatedToken(string token, HashSet<string> invalidatedTokens);
        Task Logout(string token);
    }
}
