using Route256.PriceCalculator.ConsoleApp.Interfaces;

namespace Route256.PriceCalculator.ConsoleApp;

public class Context : IContext
{
    public string GetProjectDirectory()
    {
        return Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName ?? "";
    }
}