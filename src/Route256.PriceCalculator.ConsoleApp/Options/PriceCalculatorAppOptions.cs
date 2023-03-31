namespace Route256.PriceCalculator.ConsoleApp.Options;

public sealed class PriceCalculatorAppOptions
{
    public int ParallelismDegree { get; set; }
    public int ReaderChannelBound { get; set; }
    public int WriterChannelBound { get; set; }
}