using CurrencyTrackingSystem.Domain.Entities;
using CurrencyTrackingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Xml.Linq;

namespace CurrencyTrackingSystem.BackgroundServices
{
    public class CurrencyBackgroundService : BackgroundService
    {
        private readonly ILogger<CurrencyBackgroundService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TimeSpan _updateInterval = TimeSpan.FromHours(1);

        public CurrencyBackgroundService(
            ILogger<CurrencyBackgroundService> logger,
            IServiceProvider serviceProvider,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Currency Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var xmlData = await FetchCurrencyDataAsync(stoppingToken);
                    var currencies = ParseCurrencyData(xmlData);
                    await UpdateCurrencyRatesAsync(dbContext, currencies);

                    _logger.LogInformation("Currency rates updated successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while updating currency rates.");
                }

                await Task.Delay(_updateInterval, stoppingToken);
            }
        }

        private async Task<string> FetchCurrencyDataAsync(CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("CbrApi");
            var response = await client.GetAsync("scripts/XML_daily.asp", ct);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream, Encoding.GetEncoding(1251));
            return await reader.ReadToEndAsync();
        }

        private IEnumerable<Currency> ParseCurrencyData(string xmlData)
        {
            try
            {
                var doc = XDocument.Parse(xmlData);
                var date = DateTime.Parse(doc.Root?.Attribute("Date")?.Value ?? DateTime.Now.ToString("dd.MM.yyyy"));

                return doc.Descendants("Valute")
                    .Select(v => new Currency
                    {
                        Name = v.Element("Name")?.Value ?? string.Empty,
                        Rate = decimal.Parse(v.Element("Value")?.Value ?? "0") /
                               decimal.Parse(v.Element("Nominal")?.Value ?? "1")
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse currency data.");
                throw;
            }
        }

        private async Task UpdateCurrencyRatesAsync(AppDbContext dbContext, IEnumerable<Currency> newRates)
        {
            var existingCurrencies = await dbContext.Currencies.ToDictionaryAsync(c => c.Name);

            foreach (var newRate in newRates)
            {
                if (existingCurrencies.TryGetValue(newRate.Name, out var existing))
                {
                    existing.Rate = newRate.Rate;
                    dbContext.Currencies.Update(existing);
                }
                else
                {
                    await dbContext.Currencies.AddAsync(newRate);
                }
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
