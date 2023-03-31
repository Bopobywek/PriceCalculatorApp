using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Route256.PriceCalculator.Domain.Separated;
using Route256.PriceCalculator.Domain.Services;
using Route256.PriceCalculator.Domain.Services.Interfaces;

namespace Route256.PriceCalculator.Domain;

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services.AddScoped<IPriceCalculatorService, PriceCalculatorService>(x =>
        {
            var options = x.GetRequiredService<IOptionsSnapshot<PriceCalculatorOptions>>().Value;
            var storageRepository = x.GetRequiredService<IStorageRepository>();
            return new PriceCalculatorService(options, storageRepository);
        });
        services.AddScoped<IGoodPriceCalculatorService, GoodPriceCalculatorService>();

        return services;
    }
}