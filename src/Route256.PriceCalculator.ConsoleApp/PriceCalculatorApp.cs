using System.Globalization;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Route256.PriceCalculator.ConsoleApp.Interfaces;
using Route256.PriceCalculator.ConsoleApp.Models;
using Route256.PriceCalculator.ConsoleApp.Options;
using Route256.PriceCalculator.Domain.Services.Interfaces;

namespace Route256.PriceCalculator.ConsoleApp;

public class PriceCalculatorApp
{
    private readonly IPriceCalculatorService _priceCalculatorService;
    private readonly IContext _context;
    private readonly ILogger<PriceCalculatorApp> _logger;
    private readonly PriceCalculatorAppOptions _options;
    private readonly Queue<Task> _tasks = new();
    private readonly Channel<GoodModel> _readerChannel;
    private readonly Channel<CalculationResult> _writerChannel;
    private int _numberOfLinesRead;
    private int _numberOfCalculations;
    private int _numberOfLinesWrite;
    private bool _isCompleted;

    public PriceCalculatorApp(IOptionsMonitor<PriceCalculatorAppOptions> optionsMonitor,
        IPriceCalculatorService priceCalculatorService, IContext context)
    {
        _priceCalculatorService = priceCalculatorService;
        _context = context;
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<PriceCalculatorApp>();
        _options = optionsMonitor.CurrentValue;

        _readerChannel = Channel.CreateBounded<GoodModel>(new BoundedChannelOptions(_options.ReaderChannelBound)
        {
            SingleWriter = true,
            SingleReader = false
        });
        _writerChannel = Channel.CreateBounded<CalculationResult>(
            new BoundedChannelOptions(_options.WriterChannelBound)
            {
                SingleWriter = false,
                SingleReader = true
            });
    }

    public async Task Run()
    {
        var tokenSource = new CancellationTokenSource();
        var cancellationToken = tokenSource.Token;

        var logTask = ReportProgress();

        RunProcessors(_readerChannel, _writerChannel, cancellationToken);

        var taskReadData = ReadData(
            Path.Combine(_context.GetProjectDirectory(), "data", "input.csv"),
            _readerChannel);
        var taskWriteData = WriteData(
            Path.Combine(_context.GetProjectDirectory(), "data", "output.csv"),
            _writerChannel);

        await taskReadData;
        await Task.WhenAll(_tasks);

        _writerChannel.Writer.Complete();
        await taskWriteData;

        _isCompleted = true;
        await logTask;
    }

    private void RunProcessors(Channel<GoodModel> readerChannel, Channel<CalculationResult> writerChannel,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < _options.ParallelismDegree; ++i)
        {
            var task = ProcessData(readerChannel, writerChannel, cancellationToken);
            _tasks.Enqueue(task);
            _logger.Log(LogLevel.Debug, "Task{number} created and started", i + 1);
        }
    }

    private async Task ReportProgress()
    {
        while (!_isCompleted)
        {
            _logger.Log(LogLevel.Information,
                "Read lines: {readLines}{newLine}" +
                "Process lines: {readLines}{newLine}" +
                "Write lines: {readLines}{newLine}",
                _numberOfLinesRead, Environment.NewLine, _numberOfCalculations,
                Environment.NewLine, _numberOfLinesWrite, Environment.NewLine);

            await Task.Delay(1000);
        }

        _logger.Log(LogLevel.Information,
            "Task completed{newLine}" +
            "Read lines: {readLines}{newLine}" +
            "Process lines: {readLines}{newLine}" +
            "Write lines: {readLines}{newLine}",
            Environment.NewLine, _numberOfLinesRead, Environment.NewLine, _numberOfCalculations,
            Environment.NewLine, _numberOfLinesWrite, Environment.NewLine);
    }

    private async Task ReadData(string pathToFile, Channel<GoodModel> outputChannel)
    {
        await using var fileStream = new FileStream(pathToFile, FileMode.Open, FileAccess.Read);
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

    private async Task ProcessData(Channel<GoodModel> inputChannel,
        Channel<CalculationResult> outputChannel,
        CancellationToken cancellationToken)
    {
        var random = new Random();
        cancellationToken.ThrowIfCancellationRequested();
        await foreach (var model in inputChannel.Reader.ReadAllAsync(cancellationToken))
        {
            var calculatorModel = new Domain.Models.PriceCalculator.GoodModel(
                Height: model.Height,
                Length: model.Length,
                Width: model.Width,
                Weight: model.Weight);
            decimal price = _priceCalculatorService.CalculatePrice(new[] {calculatorModel});
            // Имитируем сложность вычислений
            await Task.Delay(random.Next(150, 500), cancellationToken);
            var result = new CalculationResult(model.Id, price);

            Interlocked.Increment(ref _numberOfCalculations);
            await outputChannel.Writer.WriteAsync(result, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private async Task WriteData(string pathToFile, Channel<CalculationResult> channel)
    {
        await using var fileStream = new FileStream(pathToFile, FileMode.Create, FileAccess.Write);
        await using var streamWriter = new StreamWriter(fileStream);


        const string header = "id,delivery_price";
        await streamWriter.WriteLineAsync(header);

        await foreach (var model in channel.Reader.ReadAllAsync())
        {
            var outputLine = $"{model.Id},{model.DeliveryPrice.ToString(CultureInfo.InvariantCulture)}";

            Interlocked.Increment(ref _numberOfLinesWrite);
            await streamWriter.WriteLineAsync(outputLine);
        }
    }
}