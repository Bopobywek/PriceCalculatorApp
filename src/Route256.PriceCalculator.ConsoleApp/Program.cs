using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Route256.PriceCalculator.ConsoleApp.Interfaces;
using Route256.PriceCalculator.ConsoleApp.Options;
using Route256.PriceCalculator.Domain;
using Route256.PriceCalculator.Domain.Separated;

namespace Route256.PriceCalculator.ConsoleApp;

internal static class Program
{
    public static async Task Main()
    {
        var context = new Context();
        
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(context.GetProjectDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddTransient<PriceCalculatorApp>()
            .AddTransient<IStorageRepository, StorageStub>()
            .AddTransient<IContext, Context>()
            .Configure<PriceCalculatorAppOptions>(configuration.GetSection("PriceCalculatorAppOptions"))
            .Configure<PriceCalculatorOptions>(configuration.GetSection("PriceCalculatorOptions"))
            .AddLogging()
            .AddDomain();
        
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var app = serviceProvider.GetRequiredService<PriceCalculatorApp>();
        
        await app.Run();
    }
}