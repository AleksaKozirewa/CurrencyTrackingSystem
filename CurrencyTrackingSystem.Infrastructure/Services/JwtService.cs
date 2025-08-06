using CurrencyTrackingSystem.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTrackingSystem.Infrastructure.Services
{
    public class JwtService : IJwtService
    {
        private readonly HashSet<string> _invalidatedTokens = new();

        private readonly string _secretKey;
        private readonly string _issuer;
        private readonly int _expiryMinutes;

        public JwtService(IConfiguration configuration)
        {
            _secretKey = configuration["Jwt:SecretKey"];
            _issuer = configuration["Jwt:Issuer"];
            _expiryMinutes = int.Parse(configuration["Jwt:ExpiryMinutes"]);
        }

        public string GenerateToken(Guid userId)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, userId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _issuer,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_expiryMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public int? ValidateToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _issuer,
                    ValidAudience = _issuer,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userId = int.Parse(jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value);

                return userId;
            }
            catch
            {
                return null;
            }
        }

        public bool IsInvalidatedToken(string token, HashSet<string> invalidatedTokens)
        {
            if (invalidatedTokens.Contains(token))
            {
                return true;
            }

            return false;
        }

        public void InvalidateToken(string token)
        {
            _invalidatedTokens.Add(token);
        }
    }
}
