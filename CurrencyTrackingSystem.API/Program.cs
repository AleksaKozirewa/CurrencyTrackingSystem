using CurrencyTrackingSystem.Application.Interfaces;
using CurrencyTrackingSystem.BackgroundServices;
using CurrencyTrackingSystem.Infrastructure.Persistence;
using CurrencyTrackingSystem.Infrastructure.Services;
using CurrencyTrackingSystem.UserService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

            // Настройка JWT
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
                    };
                });

            // Настройка HttpClient для микросервисов
            builder.Services.AddHttpClient("UserService", client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Services:UserService"]);
            });

            //builder.Services.AddHttpClient("FinanceService", client =>
            //{
            //    client.BaseAddress = new Uri(builder.Configuration["Services:FinanceService"]);
            //});

            // 1. Регистрация сервисов
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddScoped<IUserService, UsersService>(); // Регистрация сервиса
            builder.Services.AddSingleton<IJwtService, JwtService>();

            // 2. Настройка Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Currency API",
                    Version = "v1",
                    Description = "API для работы с курсами валют"
                });
            });

            // Стандартная регистрация DbContext БЕЗ указания MigrationsAssembly
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null);
                        npgsqlOptions.CommandTimeout(300); // 10 минут
                    }));

            // Регистрация HttpClient для ЦБ РФ
            builder.Services.AddHttpClient("CbrApi", client =>
            {
                client.BaseAddress = new Uri("http://www.cbr.ru/");
                client.DefaultRequestHeaders.Add("User-Agent", "MyCurrencyService");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddHostedService<CurrencyBackgroundService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                try
                {
                    scope.ServiceProvider.GetRequiredService<IUserService>();
                }
                catch (Exception ex)
                {
                    throw new Exception("Ошибка конфигурации DI: " + ex.Message);
                }
            }

            app.UseAuthentication();
            app.UseAuthorization();

            // Конфигурация middleware
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Currency API v1");
                });
            }

            app.UseHttpsRedirection();
            app.MapControllers();

            // Маршрутизация запросов к микросервисам
            app.Map("/api/users/{**rest}", async (HttpContext context) =>
            {
                await ProxyRequest(context, "UserService");
            });

            async Task ProxyRequest(HttpContext context, string serviceName)
            {
                // 1. Получаем HTTP-клиент для целевого сервиса
                var clientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
                var client = clientFactory.CreateClient(serviceName);

                // 2. Создаем новый запрос к целевому сервису
                var request = new HttpRequestMessage
                {
                    Method = new HttpMethod(context.Request.Method),
                    RequestUri = new Uri($"{client.BaseAddress}{context.Request.Path}{context.Request.QueryString}")
                };

                // 3. Копируем заголовки из оригинального запроса
                foreach (var header in context.Request.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }

                // 4. Копируем тело запроса (если есть)
                if (context.Request.ContentLength > 0)
                {
                    using var ms = new MemoryStream();
                    await context.Request.Body.CopyToAsync(ms);
                    ms.Position = 0;
                    request.Content = new StreamContent(ms);
                    request.Content.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue(context.Request.ContentType);
                }

                // 5. Отправляем запрос к целевому сервису
                using var response = await client.SendAsync(request);

                // 6. Копируем ответ обратно клиенту
                context.Response.StatusCode = (int)response.StatusCode;

                foreach (var header in response.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                if (response.Content.Headers.ContentType != null)
                {
                    context.Response.ContentType = response.Content.Headers.ContentType.ToString();
                }

                await response.Content.CopyToAsync(context.Response.Body);
            }

            

            app.Run();
        }
    }
}