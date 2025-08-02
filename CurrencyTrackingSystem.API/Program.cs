using CurrencyTrackingSystem.BackgroundServices;
using CurrencyTrackingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text;

namespace CurrencyTrackingSystem.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = WebApplication.CreateBuilder(args);

            // 1. ����������� ��������
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // 2. ��������� Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Currency API",
                    Version = "v1",
                    Description = "API ��� ������ � ������� �����"
                });
            });

            // ����������� ����������� DbContext ��� �������� MigrationsAssembly
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null);
                        npgsqlOptions.CommandTimeout(300); // 10 �����
                    }));

            // ����������� HttpClient ��� �� ��
            builder.Services.AddHttpClient("CbrApi", client =>
            {
                client.BaseAddress = new Uri("http://www.cbr.ru/");
                client.DefaultRequestHeaders.Add("User-Agent", "MyCurrencyService");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddHostedService<CurrencyBackgroundService>();

            var app = builder.Build();

            // ������������ middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Currency API v1");
                });
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}