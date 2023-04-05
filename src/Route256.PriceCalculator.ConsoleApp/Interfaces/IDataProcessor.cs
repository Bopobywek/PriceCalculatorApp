using System.Threading.Channels;
using Route256.PriceCalculator.ConsoleApp.Models;

namespace Route256.PriceCalculator.ConsoleApp.Interfaces;

public interface IDataProcessor : IDataManipulator
{
    Task ProcessData(Channel<GoodModel> inputChannel, Channel<CalculationResult> outputChannel,
        CancellationToken cancellationToken);
}