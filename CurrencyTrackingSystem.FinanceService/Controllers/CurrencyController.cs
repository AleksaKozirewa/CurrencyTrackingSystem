using CurrencyTrackingSystem.Application.DTO.Finance;
using CurrencyTrackingSystem.Application.Interfaces;
using CurrencyTrackingSystem.Domain.Entities;
using CurrencyTrackingSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CurrencyTrackingSystem.FinanceService.Controllers
{
    [ApiController]
    [Route("api/currencies")]
    //[Authorize]
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;
        private readonly ILogger<CurrencyController> _logger;

        public CurrencyController(
            ICurrencyService currencyService,
            ILogger<CurrencyController> logger)
        {
            _currencyService = currencyService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCurrencies()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var currencies = await _currencyService.GetAllCurrenciesAsync(userId);
                return Ok(currencies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all currencies");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("favorites")]
        public async Task<IActionResult> GetUserFavoriteCurrencies()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var currencies = await _currencyService.GetUserFavoriteCurrenciesAsync(userId);
                return Ok(currencies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user favorite currencies");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("favorites")]
        public async Task<IActionResult> UpdateFavoriteCurrencies([FromBody] UpdateFavoriteCurrenciesDto dto)
        {
            try
            {
                var userId = dto.UserId;
                await _currencyService.UpdateFavoriteCurrenciesAsync(userId, dto);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating favorite currencies");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("healthcheck")]
        public IActionResult HealthCheck()
        {
            return Ok(new { Status = "AuthService is healthy" });
        }
    }
}
