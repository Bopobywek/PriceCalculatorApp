using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Route256.PriceCalculator.ConsoleApp.Interfaces;
using Route256.PriceCalculator.ConsoleApp.Models;
using Route256.PriceCalculator.ConsoleApp.Options;

namespace Route256.PriceCalculator.ConsoleApp;

public class PriceCalculatorApp
{
    private readonly ILogger<PriceCalculatorApp> _logger;
    private readonly IDataReader _reader;
    private readonly IDataProcessor _processor;
    private readonly IDataWriter _writer;
    private readonly PriceCalculatorAppOptions _options;
    private readonly Queue<Task> _tasks = new();
    private bool _isCompleted;
    private readonly Channel<GoodModel> _readerChannel;
    private readonly Channel<CalculationResult> _writerChannel;

    public PriceCalculatorApp(IOptionsMonitor<PriceCalculatorAppOptions> optionsMonitor,
        ILogger<PriceCalculatorApp> logger, IDataReader reader, IDataProcessor processor, IDataWriter writer)

    {
        _logger = logger;
        _reader = reader;
        _processor = processor;
        _writer = writer;
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

        var taskReadData = _reader.ReadData(_readerChannel);
        var taskWriteData = _writer.WriteData(_writerChannel);

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
            var task = _processor.ProcessData(readerChannel, writerChannel, cancellationToken);
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
                _reader.GetProcessedLines(), Environment.NewLine, _processor.GetProcessedLines(),
                Environment.NewLine, _writer.GetProcessedLines(), Environment.NewLine);

            await Task.Delay(1000);
        }

        _logger.Log(LogLevel.Information,
            "Task completed{newLine}" +
            "Read lines: {readLines}{newLine}" +
            "Process lines: {readLines}{newLine}" +
            "Write lines: {readLines}{newLine}",
            Environment.NewLine, _reader.GetProcessedLines(), Environment.NewLine,
            _processor.GetProcessedLines(), Environment.NewLine, _writer.GetProcessedLines(),
            Environment.NewLine);
    }
}