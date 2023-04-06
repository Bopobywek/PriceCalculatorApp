using Route256.PriceCalculator.Domain.Models.PriceCalculator;

namespace Route256.PriceCalculator.Domain.Separated;

public interface IGoodsRepository
{
    void AddOrUpdate(GoodAddOrUpdateModel model);
    
    ICollection<GoodAddOrUpdateModel> GetAll();
    GoodModel? Get(int id);
}