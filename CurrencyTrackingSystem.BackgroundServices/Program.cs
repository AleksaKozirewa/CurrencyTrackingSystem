using CurrencyTrackingSystem.BackgroundServices;

class Program
{
    static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddHostedService<CurrencyBackgroundService>();
    }
}