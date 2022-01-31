using CommandLine;
using FalseDotNet;
using FalseDotNet.Cli.ParserExtensions;
using Microsoft.Extensions.DependencyInjection;

return new ServiceCollection()
    .RegisterSubCommands(typeof(Program))
    .AddSingleton<ICodeParser, CodeParser>()
    .AddSingleton<ILogger, DefaultLogger>()
    .BuildServiceProvider()
    .ParseAndExecute(Parser.Default, args, _ => 1);

class DefaultLogger : ILogger
{
    public ILogger Write(string message)
    {
        Console.Write(message);
        return this;
    }

    public ILogger WriteLine(string message)
    {
        Console.WriteLine(message);
        return this;
    }
}