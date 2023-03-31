using Route256.PriceCalculator.Domain.Models.PriceCalculator;

namespace Route256.PriceCalculator.Domain.Services.Interfaces;

public interface IGoodsService
{
    IEnumerable<GoodAddOrUpdateModel> GetGoods();
}