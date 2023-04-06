using System.Threading.Channels;
using Route256.PriceCalculator.ConsoleApp.Models;

namespace Route256.PriceCalculator.ConsoleApp.Interfaces;

public interface IDataReader : IDataManipulator
{
    Task ReadData(Channel<GoodModel> outputChannel);
}