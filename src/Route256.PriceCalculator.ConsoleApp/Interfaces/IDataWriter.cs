using System.Threading.Channels;
using Route256.PriceCalculator.ConsoleApp.Models;

namespace Route256.PriceCalculator.ConsoleApp.Interfaces;

public interface IDataWriter : IDataManipulator
{
    Task WriteData(Channel<CalculationResult> inputChannel);
}