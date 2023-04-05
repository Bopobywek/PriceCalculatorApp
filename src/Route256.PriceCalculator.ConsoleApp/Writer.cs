using System.Globalization;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Route256.PriceCalculator.ConsoleApp.Interfaces;
using Route256.PriceCalculator.ConsoleApp.Models;
using Route256.PriceCalculator.ConsoleApp.Options;

namespace Route256.PriceCalculator.ConsoleApp;

public class Writer : IDataWriter
{
    private readonly string _pathToFile;
    private int _numberOfLinesWrite;

    public Writer(IOptionsSnapshot<PriceCalculatorAppOptions> options, IContext context)
    {
        _pathToFile = Path.Combine(context.GetProjectDirectory(), "data", options.Value.OutputFileName);
    }
    public int GetProcessedLines()
    {
        return _numberOfLinesWrite;
    }

    public async Task WriteData(Channel<CalculationResult> inputChannel)
    {
        await using var fileStream = new FileStream(_pathToFile, FileMode.Create, FileAccess.Write);
        await using var streamWriter = new StreamWriter(fileStream);


        const string header = "id,delivery_price";
        await streamWriter.WriteLineAsync(header);

        await foreach (var model in inputChannel.Reader.ReadAllAsync())
        {
            var outputLine = $"{model.Id},{model.DeliveryPrice.ToString(CultureInfo.InvariantCulture)}";

            Interlocked.Increment(ref _numberOfLinesWrite);
            await streamWriter.WriteLineAsync(outputLine);
        }
    }
}