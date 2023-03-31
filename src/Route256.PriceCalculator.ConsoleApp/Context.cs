using Route256.Wee4.Homework.PriceCalculatorConsoleApp.Interfaces;

namespace Route256.Wee4.Homework.PriceCalculatorConsoleApp;

public class Context : IContext
{
    public string GetProjectDirectory()
    {
        return Directory.GetParent(Environment.CurrentDirectory)?.Parent?.Parent?.FullName ?? "";
    }
}