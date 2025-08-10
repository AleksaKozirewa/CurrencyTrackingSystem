using CurrencyTrackingSystem.API.Controllers;
using CurrencyTrackingSystem.API.Filters;
using CurrencyTrackingSystem.API.Service;
using CurrencyTrackingSystem.Application.Interfaces;
using CurrencyTrackingSystem.BackgroundServices;
using CurrencyTrackingSystem.FinanceService;
using CurrencyTrackingSystem.Infrastructure.Persistence;
using Grpc.Core;
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

            builder.Services.AddMemoryCache();

            builder.Services.AddSingleton<ITokenBlacklistService, GatewayTokenBlacklistService>();

            builder.Services.AddHttpContextAccessor(); // В начало конфигурации

            // Добавьте в DI контейнер
            builder.Services.AddGrpcClient<CurrencyNewService.CurrencyNewServiceClient>(options =>
            {
                options.Address = new Uri(builder.Configuration["Grpc:FinanceServiceUrl"]);
            })
            .ConfigureChannel(o =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    // Разрешаем небезопасные соединения в development
                    var httpHandler = new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                    };
                    o.HttpHandler = httpHandler;
                    o.Credentials = ChannelCredentials.Insecure;
                }
            });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
                    //ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var tokenBlacklistService = context.HttpContext.RequestServices
                            .GetRequiredService<ITokenBlacklistService>();

                        var token = context.Request.Headers["Authorization"]
                            .ToString()
                            .Replace("Bearer ", "");

                        if (await tokenBlacklistService.IsTokenBlacklistedAsync(token))
                        {
                            context.Fail("Token has been revoked");
                        }
                    },
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("requireJwtToken", policy =>
                    policy.RequireAuthenticatedUser());
            });

            // Добавление YARP
            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            builder.Services.AddControllers();
            builder.Services.AddControllers().AddApplicationPart(typeof(CurrencyController).Assembly);
            builder.Services.AddEndpointsApiExplorer();

            // Настройка Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Currency Tracking System API",
                    Version = "v1",
                    Description = "Основной API для системы отслеживания валют"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Важно для YARP!
                c.DocumentFilter<YarpSwaggerFilter>();
            });

            // Регистрация HttpClient для ЦБ РФ
            builder.Services.AddHttpClient("CbrApi", client =>
            {
                client.BaseAddress = new Uri("http://www.cbr.ru/");
                client.DefaultRequestHeaders.Add("User-Agent", "MyCurrencyService");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddHostedService<CurrencyBackgroundService>();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Currency Tracking System API");
                    // Для работы через IIS Express прокси
                    c.RoutePrefix = "swagger";
                });
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseExceptionHandler("/error");

            app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/swagger"), appBuilder =>
            {
                appBuilder.UseEndpoints(endpoints => endpoints.MapReverseProxy());
            });

            app.MapControllers();

            app.Run();
        }
    }
}