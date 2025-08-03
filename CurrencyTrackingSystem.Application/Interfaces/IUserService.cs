using CurrencyTrackingSystem.Application.DTO.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTrackingSystem.Application.Interfaces
{
    public interface IUserService
    {
        Task<AuthResult> RegisterUserAsync(UserRegistrationDto dto);
        Task<string> LoginAsync(UserLoginDto dto);
    }
}
