using CurrencyTrackingSystem.BackgroundServices;
using CurrencyTrackingSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

class Program
{
    static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<CurrencyBackgroundService>();
    }
}

//var host = builder.Build();
//host.Run();