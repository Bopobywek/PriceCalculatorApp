using System.Globalization;
using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Route256.PriceCalculator.ConsoleApp.Interfaces;
using Route256.PriceCalculator.ConsoleApp.Models;
using Route256.PriceCalculator.ConsoleApp.Options;

namespace Route256.PriceCalculator.ConsoleApp;

public class Reader : IDataReader
{
    private readonly string _pathToFile;
    private int _numberOfLinesRead;

    public Reader(IOptionsSnapshot<PriceCalculatorAppOptions> options, IContext context)
    {
        _pathToFile = Path.Combine(context.GetProjectDirectory(), "data", options.Value.InputFileName);
    }

    public async Task ReadData(Channel<GoodModel> outputChannel)
    {
        await using var fileStream = new FileStream(_pathToFile, FileMode.Open, FileAccess.Read);
        using var streamReader = new StreamReader(fileStream);

        var _ = await streamReader.ReadLineAsync();

        var lineIndex = 0;
        while (!streamReader.EndOfStream)
        {
            var line = await streamReader.ReadLineAsync();
            var tokens = line?.Split(",",
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (tokens == null)
            {
                throw new FormatException($"The input file line {lineIndex + 1} cannot be parsed");
            }

            if (tokens.Length != 5)
            {
                throw new FormatException($"In line {lineIndex + 1} of input file: " +
                                          $"{tokens.Length} columns are presented, but 5 are expected");
            }

            var model = new GoodModel(
                long.Parse(tokens[0], CultureInfo.InvariantCulture),
                int.Parse(tokens[1], CultureInfo.InvariantCulture),
                int.Parse(tokens[2], CultureInfo.InvariantCulture),
                int.Parse(tokens[3], CultureInfo.InvariantCulture),
                int.Parse(tokens[4], CultureInfo.InvariantCulture));

            Interlocked.Increment(ref _numberOfLinesRead);
            await outputChannel.Writer.WriteAsync(model);

            ++lineIndex;
        }

        outputChannel.Writer.Complete();
    }

    public int GetProcessedLines()
    {
        return _numberOfLinesRead;
    }
}