using System.CommandLine;
using System.CommandLine.Invocation;
using System.Drawing;
using FalseDotNet.Interpret;
using FalseDotNet.Parse;
using FalseDotNet.Utility;
using Microsoft.Extensions.DependencyInjection;
using Pastel;

namespace FalseDotNet.Cli.SubCommands;

public class RunCommandHandler(
    Option<FileInfo?> inputPathOption,
    Option<TypeSafety> typeSafetyOption,
    Option<bool> printOperationsOption,
    Argument<FileInfo> pathArgument
) : ICommandHandler
{
    public int Invoke(InvocationContext context)
    {
        return InvokeAsync(context).GetAwaiter().GetResult();
    }

    public Task<int> InvokeAsync(InvocationContext context)
    {
        var inputPath = context.ParseResult.GetValueForOption(inputPathOption);
        var typeSafety = context.ParseResult.GetValueForOption(typeSafetyOption);
        var printOperations = context.ParseResult.GetValueForOption(printOperationsOption);
        var path = context.ParseResult.GetValueForArgument(pathArgument);

        var services = new ServiceCollection()
            .AddSingleton<ILogger, DefaultLogger>()
            .AddTransient<IIdGenerator, IncrementingIdGenerator>()
            .AddTransient<ICodeParser, CodeParser>()
            .AddTransient<IInterpreter, Interpreter>()
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger>();
        var codeParser = services.GetRequiredService<ICodeParser>();
        var interpreter = services.GetRequiredService<IInterpreter>();

        try
        {
            using var sr = path.OpenText();
            using var input = inputPath?.OpenText();
            var code = sr.ReadToEnd();
            var parsedCode = codeParser.Parse(code);
            interpreter.Interpret(
                parsedCode,
                new InterpreterConfig
                {
                    PrintOperations = printOperations,
                    TypeSafety = typeSafety,
                },
                input
            );
        }
        catch (InterpreterException exception)
        {
            logger.WriteLine(exception.Message.Pastel(Color.IndianRed));
        }
        catch (IOException e)
        {
            logger.WriteLine($"Exception while reading [{path}]:".Pastel(Color.IndianRed));
            logger.WriteLine(e.Message.Pastel(Color.IndianRed));
            return Task.FromResult(1);
        }

        return Task.FromResult(0);
    }
}
