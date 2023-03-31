namespace Route256.PriceCalculator.Domain.Models.PriceCalculator;

public record GoodAddOrUpdateModel(
    string Name,
    int Id,
    int Height,
    int Length,
    int Width,
    int Weight,
    int Count,
    decimal Price);