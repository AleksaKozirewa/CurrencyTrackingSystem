namespace CurrencyTrackingSystem.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(Guid userId);
        Guid ValidateToken(string token);
        bool IsInvalidatedToken(string token, HashSet<string> invalidatedTokens);
        void InvalidateToken(string token);
    }
}
