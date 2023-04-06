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
    private readonly Dictionary<Task, CancellationTokenSource> _cancellationTokenSources = new();
    private readonly Queue<Task> _tasks = new();
    private bool _isCompleted;
    private readonly Channel<GoodModel> _readerChannel;
    private readonly Channel<CalculationResult> _writerChannel;
    private bool _isProcessorsFinishRead;

    public PriceCalculatorApp(IOptionsMonitor<PriceCalculatorAppOptions> optionsMonitor,
        ILogger<PriceCalculatorApp> logger, IDataReader reader, IDataProcessor processor, IDataWriter writer)

    {
        _logger = logger;
        _reader = reader;
        _processor = processor;
        _writer = writer;
        _options = optionsMonitor.CurrentValue;
        
        optionsMonitor.OnChange(options => { ChangeNumberOfProcessors(options.ParallelismDegree); });

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
        var logTask = ReportProgress();

        RunProcessors(_options.ParallelismDegree, _readerChannel, _writerChannel);

        var readDataTask = _reader.ReadData(_readerChannel);
        var writeDataTask = _writer.WriteData(_writerChannel);

        await readDataTask;

        await _readerChannel.Reader.Completion;
        
        // Чтобы не получилось такой ситуации, что при Task.WhenAll список задач изменяется,
        // ставим флаг.
        lock (_tasks)
        {
            _isProcessorsFinishRead = true;
        }

        await Task.WhenAll(_tasks);

        _writerChannel.Writer.Complete();
        await writeDataTask;

        _isCompleted = true;
        await logTask;

    }

    private void ChangeNumberOfProcessors(int newCount)
    {
        lock (_tasks)
        {
            if (_isProcessorsFinishRead)
            {
                return;
            }
    
            if (newCount > _tasks.Count)
            {
                var delta = newCount - _tasks.Count;
                RunProcessors(delta, _readerChannel, _writerChannel);
            }
            else if (newCount < _tasks.Count)
            {
                var delta = _tasks.Count - newCount;
                for (var i = 0; i < delta; ++i)
                {
                    var task = _tasks.Dequeue();
                    _cancellationTokenSources[task].Cancel();
                    _logger.Log(LogLevel.Debug, "Task{number} canceled", _tasks.Count + 1);
                }
            }
        }
    }
    
    private void RunProcessors(int number, Channel<GoodModel> readerChannel, Channel<CalculationResult> writerChannel)
    {
        for (var i = 0; i < number; ++i)
        {
            var tokenSource = new CancellationTokenSource();
            var cancellationToken = tokenSource.Token;
            
            var task = _processor.ProcessData(readerChannel, writerChannel, cancellationToken);
            _tasks.Enqueue(task);
            _cancellationTokenSources[task] = tokenSource;
            _logger.Log(LogLevel.Debug, "Task{number} created and started", _tasks.Count);
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