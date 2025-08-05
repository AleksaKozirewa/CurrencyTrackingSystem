using CurrencyTrackingSystem.API.Controllers;
using CurrencyTrackingSystem.API.Filters;
using CurrencyTrackingSystem.Application.Interfaces;
using CurrencyTrackingSystem.BackgroundServices;
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

namespace CurrencyTrackingSystem.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = WebApplication.CreateBuilder(args);

            // Добавление YARP
            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            // Настройка JWT
            //builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //    .AddJwtBearer(options =>
            //    {
            //        options.TokenValidationParameters = new TokenValidationParameters
            //        {
            //            ValidateIssuer = true,
            //            ValidateAudience = true,
            //            ValidateLifetime = true,
            //            ValidateIssuerSigningKey = true,
            //            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            //            ValidAudience = builder.Configuration["Jwt:Audience"],
            //            IssuerSigningKey = new SymmetricSecurityKey(
            //                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
            //        };
            //    });

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
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
                        Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"])),
                    ClockSkew = TimeSpan.Zero
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

            // Настройка HttpClient для микросервисов
            //builder.Services.AddHttpClient("UserService", client =>
            //{
            //    client.BaseAddress = new Uri(builder.Configuration["Services:UserService"]);
            //});

            //builder.Services.AddHttpClient("FinanceService", client =>
            //{
            //    client.BaseAddress = new Uri(builder.Configuration["Services:FinanceService"]);
            //});

            // 1. Регистрация сервисов
            //builder.Services.AddAutoMapper(typeof(Program));
            builder.Services.AddControllers();
            builder.Services.AddControllers()
    .AddApplicationPart(typeof(CurrencyController).Assembly);
            builder.Services.AddEndpointsApiExplorer();
            //builder.Services.AddSwaggerGen();

            //builder.Services.AddScoped<IUserService, UsersService>(); // Регистрация сервиса
            //builder.Services.AddSingleton<IJwtService, JwtService>();
            //builder.Services.AddScoped<ICurrencyService, CurrencyService>();
            //builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
            //builder.Services.AddMemoryCache();
            //builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();



            // 2. Настройка Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Currency Tracking System API",
                    Version = "v1",
                    Description = "Основной API для системы отслеживания валют"
                });

                //c.SwaggerDoc("finance", new OpenApiInfo { Title = "Finance API", Version = "v1" });

                //c.SwaggerDoc("gateway", new OpenApiInfo
                //{
                //    Title = "API Gateway v1",
                //    Version = "v1",
                //    Description = "API Gateway v1"
                //});

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

            //// Стандартная регистрация DbContext БЕЗ указания MigrationsAssembly
            //builder.Services.AddDbContext<AppDbContext>(options =>
            //    options.UseNpgsql(
            //        builder.Configuration.GetConnectionString("DefaultConnection"),
            //        npgsqlOptions =>
            //        {
            //            npgsqlOptions.EnableRetryOnFailure(
            //                maxRetryCount: 5,
            //                maxRetryDelay: TimeSpan.FromSeconds(30),
            //                errorCodesToAdd: null);
            //            npgsqlOptions.CommandTimeout(300); // 10 минут
            //        }));

            // Регистрация HttpClient для ЦБ РФ
            builder.Services.AddHttpClient("CbrApi", client =>
            {
                client.BaseAddress = new Uri("http://www.cbr.ru/");
                client.DefaultRequestHeaders.Add("User-Agent", "MyCurrencyService");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            builder.Services.AddHostedService<CurrencyBackgroundService>();
            builder.Services.AddHostedService<TokenCleanupBackgroundService>();

            // Добавьте эту регистрацию перед builder.Build()
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            var app = builder.Build();

            //if (app.Environment.IsDevelopment())
            //{
            //    using var scope = app.Services.CreateScope();
            //    try
            //    {
            //        scope.ServiceProvider.GetRequiredService<IUserService>();
            //    }
            //    catch (Exception ex)
            //    {
            //        throw new Exception("Ошибка конфигурации DI: " + ex.Message);
            //    }
            //}



            //app.MapWhen(ctx => !ctx.Request.Path.StartsWithSegments("/swagger"), appBuilder =>
            //{
            //    appBuilder.UseRouting();
            //    appBuilder.UseEndpoints(endpoints =>
            //    {
            //        endpoints.MapReverseProxy();
            //    });
            //});

            // Конфигурация middleware
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

            app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/swagger"), appBuilder =>
            {
                appBuilder.UseRouting();
                appBuilder.UseEndpoints(endpoints =>
                {
                    endpoints.MapReverseProxy();
                });
            });

            //if (app.Environment.IsDevelopment())
            //{
            //    app.UseSwagger();
            //    app.UseSwaggerUI(c => {
            //        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway v1");
            //        // Для работы через IIS Express прокси
            //        c.RoutePrefix = "swagger";
            //    });
            //}

            app.UseHttpsRedirection();
            //app.UseAuthentication();
            //app.UseAuthorization();
            app.MapControllers();

            app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

            //app.MapReverseProxy();

            // Маршрутизация запросов к микросервисам
            //app.Map("/api/users/{**rest}", async (HttpContext context) =>
            //{
            //    await ProxyRequest(context, "UserService");
            //});

            //async Task ProxyRequest(HttpContext context, string serviceName)
            //{
            //    // 1. Получаем HTTP-клиент для целевого сервиса
            //    var clientFactory = context.RequestServices.GetRequiredService<IHttpClientFactory>();
            //    var client = clientFactory.CreateClient(serviceName);

            //    // 2. Создаем новый запрос к целевому сервису
            //    var request = new HttpRequestMessage
            //    {
            //        Method = new HttpMethod(context.Request.Method),
            //        RequestUri = new Uri($"{client.BaseAddress}{context.Request.Path}{context.Request.QueryString}")
            //    };

            //    // 3. Копируем заголовки из оригинального запроса
            //    foreach (var header in context.Request.Headers)
            //    {
            //        request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            //    }

            //    // 4. Копируем тело запроса (если есть)
            //    if (context.Request.ContentLength > 0)
            //    {
            //        using var ms = new MemoryStream();
            //        await context.Request.Body.CopyToAsync(ms);
            //        ms.Position = 0;
            //        request.Content = new StreamContent(ms);
            //        request.Content.Headers.ContentType =
            //            new System.Net.Http.Headers.MediaTypeHeaderValue(context.Request.ContentType);
            //    }

            //    // 5. Отправляем запрос к целевому сервису
            //    using var response = await client.SendAsync(request);

            //    // 6. Копируем ответ обратно клиенту
            //    context.Response.StatusCode = (int)response.StatusCode;

            //    foreach (var header in response.Headers)
            //    {
            //        context.Response.Headers[header.Key] = header.Value.ToArray();
            //    }

            //    if (response.Content.Headers.ContentType != null)
            //    {
            //        context.Response.ContentType = response.Content.Headers.ContentType.ToString();
            //    }

            //    await response.Content.CopyToAsync(context.Response.Body);
            //}



            app.Run();
        }
    }
}