using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Route256.PriceCalculator.Domain;
using Route256.PriceCalculator.Domain.Separated;
using Route256.Wee4.Homework.PriceCalculatorConsoleApp.Interfaces;
using Route256.Wee4.Homework.PriceCalculatorConsoleApp.Options;

namespace Route256.Wee4.Homework.PriceCalculatorConsoleApp;

internal static class Program
{
    public static async Task Main()
    {
        var context = new Context();
        
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(context.GetProjectDirectory())
            .AddJsonFile("appsettings.json", optional: false)
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