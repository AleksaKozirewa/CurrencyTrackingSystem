using CurrencyTrackingSystem.Application.Interfaces;
using CurrencyTrackingSystem.Domain.Interfaces;
using CurrencyTrackingSystem.Infrastructure.Persistence;
using CurrencyTrackingSystem.Infrastructure.Repositories;
using CurrencyTrackingSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using AutoMapper;

namespace CurrencyTrackingSystem.FinanceService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Регистрация сервисов
            builder.Services.AddScoped<ICurrencyService, CurrencyService>();
            builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();

            // Конфигурация сервисов
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            //builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //.AddJwtBearer(options =>
            //{
            //    options.TokenValidationParameters = new TokenValidationParameters
            //    {
            //        ValidateIssuerSigningKey = true,
            //        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            //            builder.Configuration["Jwt:SecretKey"])),
            //        ValidateIssuer = false,
            //        ValidateAudience = false
            //    };
            //});

            //builder.Services.AddAuthentication(options =>
            //{
            //    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            //    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            //})
            //.AddJwtBearer(options =>
            //{
            //    options.TokenValidationParameters = new TokenValidationParameters
            //    {
            //        ValidateIssuer = true,
            //        ValidateAudience = true,
            //        ValidateLifetime = true,
            //        ValidateIssuerSigningKey = true,
            //        ValidIssuer = builder.Configuration["Jwt:Issuer"],
            //        ValidAudience = builder.Configuration["Jwt:Audience"],
            //        IssuerSigningKey = new SymmetricSecurityKey(
            //            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])),
            //        ClockSkew = TimeSpan.Zero
            //    };

            //    options.Events = new JwtBearerEvents
            //    {
            //        OnTokenValidated = async context =>
            //        {
            //            var tokenBlacklistService = context.HttpContext.RequestServices
            //                .GetRequiredService<ITokenBlacklistService>();

            //            var token = context.Request.Headers["Authorization"]
            //                .ToString()
            //                .Replace("Bearer ", "");

            //            if (await tokenBlacklistService.IsTokenBlacklistedAsync(token))
            //            {
            //                context.Fail("Token has been revoked");
            //            }
            //        },
            //        OnAuthenticationFailed = context =>
            //        {
            //            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            //            {
            //                context.Response.Headers.Add("Token-Expired", "true");
            //            }
            //            return Task.CompletedTask;
            //        }
            //    };
            //});

            //builder.Services.AddAuthorization();

            //builder.Services.AddSwaggerGen();

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Finance Service", Version = "v1" });
            });

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("finance", new OpenApiInfo
                {
                    Title = "Finance Service API",
                    Version = "v1",
                    Description = "Provides currency rates and financial data"
                });

                //c.SwaggerDoc("v2", new OpenApiInfo
                //{
                //    Title = "Finance Service API",
                //    Version = "v2",
                //    Description = "New version with additional features"
                //});

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header
                });

                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    if (docName == "v1")
                        return !apiDesc.RelativePath.Contains("v2");
                    //if (docName == "v2")
                    //    return apiDesc.RelativePath.Contains("v2");
                    return true;
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

            var app = builder.Build();

            // Middleware должен быть подключен ДО MapControllers()
            app.UseSwagger(); // Генерирует /swagger/v1/swagger.json
            app.UseSwaggerUI(); // Не требуется для API Gateway

            // Конфигурация middleware
            app.UseAuthorization();
            app.MapControllers();



            app.Run();
        }
    }
}