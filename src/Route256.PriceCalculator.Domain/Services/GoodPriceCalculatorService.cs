using Route256.PriceCalculator.Domain.Separated;
using Route256.PriceCalculator.Domain.Services.Interfaces;

namespace Route256.PriceCalculator.Domain.Services;

internal sealed class GoodPriceCalculatorService : IGoodPriceCalculatorService
{
    private readonly IGoodsRepository _goodsRepository;
    private readonly IPriceCalculatorService _priceCalculatorService;

    public GoodPriceCalculatorService(
        IGoodsRepository goodsRepository,
        IPriceCalculatorService priceCalculatorService)
    {
        _goodsRepository = goodsRepository;
        _priceCalculatorService = priceCalculatorService;
    }

    public decimal CalculatePrice(
        int goodId,
        decimal distance)
    {
        var goodModel = _goodsRepository.Get(goodId);

        if (goodModel is null)
        {
            throw new KeyNotFoundException($"Товара с id = {goodId} не существует.");
        }

        return _priceCalculatorService.CalculatePrice(new[] {goodModel}) * distance;
    }
}