using CurrencyTrackingSystem.Application.DTO.User;
using CurrencyTrackingSystem.Application.Interfaces;
using CurrencyTrackingSystem.Domain.Entities;
using CurrencyTrackingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTrackingSystem.UserService.Services
{
    public class UsersService : IUserService
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;

        public UsersService(
            AppDbContext context,
            IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<AuthResult> RegisterUserAsync(UserRegistrationDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Name == dto.Username))
                return new AuthResult { Success = false, ErrorMessage = "Username already exists" };

            var user = new User
            {
                Name = dto.Username,
                PasswordHash = dto.Password
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return new AuthResult { Success = true, UserId = user.Id };
        }

        public async Task<string> LoginAsync(UserLoginDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Name == dto.Username);

            if (user == null || !string.Equals(dto.Password, user.PasswordHash))
                return null;

            return _jwtService.GenerateToken(user.Id);
        }
    }
}
