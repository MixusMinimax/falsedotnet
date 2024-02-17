using System.CommandLine;
using System.CommandLine.Parsing;
using System.Drawing;
using CommandLine;
using FalseDotNet.Binary;
using FalseDotNet.Cli;
using FalseDotNet.Cli.SubCommandExtensions;
using FalseDotNet.Compile;
using FalseDotNet.Compile.Optimization;
using FalseDotNet.Interpret;
using FalseDotNet.Parse;
using FalseDotNet.Utility;
using Microsoft.Extensions.DependencyInjection;
using Pastel;

// return await new ServiceCollection()
//     .RegisterSubCommands(typeof(Program))
//     .AddSingleton<ILogger, DefaultLogger>()
//     .AddTransient<IIdGenerator, IncrementingIdGenerator>()
//     .AddSingleton<IPathConverter, PathConverter>()
//     .AddSingleton<ILinuxExecutor, LinuxExecutor>()
//     .AddTransient<ICodeParser, CodeParser>()
//     .AddTransient<IInterpreter, Interpreter>()
//     .AddTransient<ICompiler, Compiler>()
//     .AddTransient<IOptimizer, Optimizer>()
//     .BuildServiceProvider()
//     .ParseAndExecute(new Parser(with =>
//     {
//         with.AutoHelp = Parser.Default.Settings.AutoHelp;
//         with.AutoVersion = Parser.Default.Settings.AutoVersion;
//         with.HelpWriter = Parser.Default.Settings.HelpWriter;
//         with.CaseInsensitiveEnumValues = true;
//     }), args, _ => 1);

namespace FalseDotNet.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var inputPathOption = new Option<FileInfo?>(
            aliases: ["--input", "-i"],
            description: "Read from file instead of stdin for program input.",
            isDefault: true,
            parseArgument: ParseFileInfo
        );

        var typeSafetyOption = new Option<TypeSafety>(
            aliases: ["--type-safety", "-t"],
            description: "What level of type safety to enforce.\n"
                         + "LAMBDA only enforces lambda execution, but allows integers\n"
                         + "to work as references, since they are masked anyway.",
            getDefaultValue: () => TypeSafety.None
        );

        var printOperationsOption = new Option<bool>(
            aliases: ["--print-operations", "-p"],
            description: "Print operations before executing them."
        );

        // positional
        var pathOption = new Argument<FileInfo>(
            name: "PATH",
            description: "File containing FALSE code.",
            parse: result => ParseFileInfo(result)!
        );

        var runCommand = new Command("run", "Interpret a False program")
        {
            inputPathOption,
            typeSafetyOption,
            printOperationsOption,
            pathOption
        };
        runCommand.SetHandler(
            (inputPath, typeSafety, printOperations, path) =>
            {
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
                    interpreter.Interpret(parsedCode, new InterpreterConfig
                    {
                        PrintOperations = printOperations,
                        TypeSafety = typeSafety,
                    }, input);
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
            },
            inputPathOption,
            typeSafetyOption,
            printOperationsOption,
            pathOption
        );

        var rootCommand = new RootCommand("FalseDotNet CLI") { runCommand };

        return await rootCommand.InvokeAsync(args);
    }

    private static FileInfo? ParseFileInfo(SymbolResult result)
    {
        var pathString = result.Tokens.FirstOrDefault();
        if (pathString is null)
            return null;
        var path = new FileInfo(pathString.Value);
        if (path.Exists)
            return path;
        result.ErrorMessage = $"File {path.FullName} does not exist.";
        return null;
    }
}
