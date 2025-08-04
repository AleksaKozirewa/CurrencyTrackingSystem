using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTrackingSystem.Application.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(Guid userId);
        int? ValidateToken(string token);
        bool IsInvalidatedToken(string token, HashSet<string> invalidatedTokens);
        void InvalidateToken(string token);
    }
}
