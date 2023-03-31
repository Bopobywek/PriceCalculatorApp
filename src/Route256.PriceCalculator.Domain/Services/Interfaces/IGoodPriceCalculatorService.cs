namespace Route256.PriceCalculator.Domain.Services.Interfaces;

public interface IGoodPriceCalculatorService
{
    decimal CalculatePrice(
        int goodId, 
        decimal distance);
}