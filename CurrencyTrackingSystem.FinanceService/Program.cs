using CurrencyTrackingSystem.Application.Interfaces;
using CurrencyTrackingSystem.Domain.Interfaces;
using CurrencyTrackingSystem.FinanceService.Service;
using CurrencyTrackingSystem.Infrastructure.Persistence;
using CurrencyTrackingSystem.Infrastructure.Repositories;
using CurrencyTrackingSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Net;

namespace CurrencyTrackingSystem.FinanceService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCors(o => o.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
            }));

            // Отключаем требования HTTPS в development
            if (builder.Environment.IsDevelopment())
            {
                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.Listen(IPAddress.Any, 6003, listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http2; // HTTP/2 без HTTPS
                    });
                });
            }

            // Регистрация сервисов
            builder.Services.AddScoped<ICurrencyService, CurrencyService>();
            builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
            builder.Services.AddScoped<IJwtService, JwtService>();

            builder.Services.AddControllers();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddGrpc();

            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("finance", new OpenApiInfo
                {
                    Title = "Finance Service API",
                    Version = "v1",
                    Description = "Сервис финансов"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header
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
            });

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])),
                        ValidateIssuer = false, // Микросервис доверяет Gateway
                        ValidateAudience = false
                    };
                });


            builder.Services.AddAuthorization();

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

            app.UseCors("AllowAll");
            app.UseSwagger(); 

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/finance/swagger.json", "Finance Service API v1");
                c.RoutePrefix = "swagger";
            });

            app.UseAuthorization();
            app.MapControllers();

            // Настраиваем gRPC endpoint
            app.MapGrpcService<CurrencyGrpcService>();
            app.MapGet("/", () => "Finance gRPC Service is running.");

            app.Run();
        }
    }
}