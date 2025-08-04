using CurrencyTrackingSystem.Application.DTO.User;
using CurrencyTrackingSystem.Application.Interfaces;
using CurrencyTrackingSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyTrackingSystem.UserService.Controllers
{
    [ApiController]
    [Route("api/user")]
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

        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // В JWT логаут реализуется на клиенте путём удаления токена
            // Здесь можно добавить логирование выхода
            //_logger.LogInformation("Пользователь вышел из системы");
            //return Ok(new { Message = "Logout successful" });

            var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            _tokenBlacklistService.BlacklistTokenAsync(token);
            //_userService.Logout(token);

            return Ok(new { Message = "Token invalidated" });
        }

        [HttpGet("healthcheck")]
        public IActionResult HealthCheck()
        {
            return Ok(new { Status = "AuthService is healthy" });
        }
    }
}
