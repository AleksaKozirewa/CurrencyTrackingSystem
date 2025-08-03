using CurrencyTrackingSystem.Application.Interfaces;
using CurrencyTrackingSystem.UserService.Services;

namespace CurrencyTrackingSystem.UserService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Конфигурация сервисов
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();

            

            var app = builder.Build();

            // Конфигурация middleware
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
