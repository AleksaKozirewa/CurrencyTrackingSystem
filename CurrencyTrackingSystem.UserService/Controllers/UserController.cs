using CurrencyTrackingSystem.Application.DTO.User;
using CurrencyTrackingSystem.Application.Interfaces;
using CurrencyTrackingSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CurrencyTrackingSystem.UserService.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenBlacklistService _tokenBlacklistService;
        private readonly ILogger<UserController> _logger;

        public UserController(
            IUserService userService,
            ITokenBlacklistService tokenBlacklistService,
            ILogger<UserController> logger)
        {
            _userService = userService;
            _tokenBlacklistService = tokenBlacklistService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegistrationDto dto)
        {
            try
            {
                _logger.LogInformation("Регистрация нового пользователя: {Username}", dto.Username);
                var result = await _userService.RegisterUserAsync(dto);

                if (!result.Success)
                {
                    _logger.LogWarning("Ошибка регистрации: {Error}", result.ErrorMessage);
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации пользователя");
                return StatusCode(500, "Internal server error");
            }
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto dto)
        {
            try
            {
                _logger.LogInformation("Попытка входа пользователя: {Username}", dto.Username);
                var token = await _userService.LoginAsync(dto);

                if (string.IsNullOrEmpty(token))
                    return Unauthorized();

                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе пользователя");
                return StatusCode(500, "Internal server error");
            }
        }

        [Authorize]
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            // Добавляем токен в черный список
            _tokenBlacklistService.BlacklistTokenAsync(token);

            // Логируем выход
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("User {UserId} logged out. Token invalidated.", userId);

            return Ok(new
            {
                Message = "Logout successful",
                Details = "Token has been invalidated. Please remove it from client side."
            });
        }
    }
}
