using CurrencyTrackingSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

namespace CurrencyTrackingSystem.API.Controllers
{
    [ApiController]
    [Route("api/currencies")]
    public class CurrencyController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CurrencyController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Получить все валюты
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var currencies = await _context.Currencies
                .OrderBy(c => c.Name)
                .ToListAsync();

            return Ok(currencies);
        }
    }
}
