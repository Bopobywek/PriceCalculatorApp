namespace Route256.Wee4.Homework.PriceCalculatorConsoleApp.Options;

public sealed class PriceCalculatorAppOptions
{
    public int ParallelismDegree { get; set; }
    public int ReaderChannelBound { get; set; }
    public int WriterChannelBound { get; set; }
}