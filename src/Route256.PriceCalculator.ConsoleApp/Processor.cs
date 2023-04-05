using System.Threading.Channels;
using Route256.PriceCalculator.ConsoleApp.Interfaces;
using Route256.PriceCalculator.ConsoleApp.Models;
using Route256.PriceCalculator.Domain.Services.Interfaces;

namespace Route256.PriceCalculator.ConsoleApp;

public class Processor : IDataProcessor
{
    private readonly IPriceCalculatorService _priceCalculatorService;
    private int _numberOfCalculations;

    public Processor(IPriceCalculatorService priceCalculatorService)
    {
        _priceCalculatorService = priceCalculatorService;
    }

    public int GetProcessedLines()
    {
        return _numberOfCalculations;
    }

    public async Task ProcessData(Channel<GoodModel> inputChannel,
        Channel<CalculationResult> outputChannel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await foreach (var model in inputChannel.Reader.ReadAllAsync(cancellationToken))
        {
            var calculatorModel = new Domain.Models.PriceCalculator.GoodModel(
                Height: model.Height,
                Length: model.Length,
                Width: model.Width,
                Weight: model.Weight);
            decimal price = _priceCalculatorService.CalculatePrice(new[] {calculatorModel});
            var result = new CalculationResult(model.Id, price);

            Interlocked.Increment(ref _numberOfCalculations);
            await outputChannel.Writer.WriteAsync(result, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}