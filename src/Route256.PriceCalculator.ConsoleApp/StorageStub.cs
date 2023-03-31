using System.Collections.Immutable;
using Route256.PriceCalculator.Domain.Entities;
using Route256.PriceCalculator.Domain.Separated;

namespace Route256.Wee4.Homework.PriceCalculatorConsoleApp;

public class StorageStub : IStorageRepository
{
    public void Save(StorageEntity entity)
    {
    }

    public IReadOnlyList<StorageEntity> Query()
    {
        return ImmutableList<StorageEntity>.Empty;
    }
}