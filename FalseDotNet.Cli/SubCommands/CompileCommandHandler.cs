using System.CommandLine;
using System.CommandLine.Invocation;
using System.Drawing;
using System.Text.RegularExpressions;
using FalseDotNet.Binary;
using FalseDotNet.Compile;
using FalseDotNet.Compile.Optimization;
using FalseDotNet.Parse;
using FalseDotNet.Utility;
using Microsoft.Extensions.DependencyInjection;
using Pastel;

namespace FalseDotNet.Cli.SubCommands;

public partial class CompileCommandHandler(
    Option<FileInfo?> inputPathOption,
    Option<FileInfo?> outputPathOption,
    Option<TypeSafety> typeSafetyOption,
    Option<OptimizerConfig.EOptimizationLevel> optimizationLevelOption,
    Option<bool> assembleOption,
    Option<bool> linkOption,
    Option<bool> runOption,
    Argument<FileInfo> pathArgument
) : ICommandHandler
{
    public int Invoke(InvocationContext context)
    {
        return InvokeAsync(context).GetAwaiter().GetResult();
    }

    public async Task<int> InvokeAsync(InvocationContext context)
    {
        var inputPath = context.ParseResult.GetValueForOption(inputPathOption);
        var outputPath = context.ParseResult.GetValueForOption(outputPathOption);
        var typeSafety = context.ParseResult.GetValueForOption(typeSafetyOption);
        var optimizationLevel = context.ParseResult.GetValueForOption(optimizationLevelOption);
        var assemble = context.ParseResult.GetValueForOption(assembleOption);
        var link = context.ParseResult.GetValueForOption(linkOption);
        var run = context.ParseResult.GetValueForOption(runOption);
        var path = context.ParseResult.GetValueForArgument(pathArgument);

        outputPath ??= new FileInfo(OutputPathRegex().Replace(path.FullName, "$1.asm"));
        if (path.FullName == outputPath.FullName)
            throw new ArgumentException("Input and Output path point to the same file!");
        outputPath.Directory?.Create();

        var objectPath = new FileInfo(AsmRegex().Replace(outputPath.FullName, ".o"));
        var binaryPath = new FileInfo(AsmRegex().Replace(outputPath.FullName, ""));

        var services = new ServiceCollection()
            .AddSingleton<ILogger, DefaultLogger>()
            .AddTransient<IIdGenerator, IncrementingIdGenerator>()
            .AddSingleton<IPathConverter, PathConverter>()
            .AddSingleton<ILinuxExecutor, LinuxExecutor>()
            .AddTransient<ICodeParser, CodeParser>()
            .AddTransient<ICompiler, Compiler>()
            .AddTransient<IOptimizer, Optimizer>()
            .BuildServiceProvider();

        var logger = services.GetRequiredService<ILogger>();
        var codeParser = services.GetRequiredService<ICodeParser>();
        var compiler = services.GetRequiredService<ICompiler>();
        var pathConverter = services.GetRequiredService<IPathConverter>();
        var executor = services.GetRequiredService<ILinuxExecutor>();

        try
        {
            using var sr = path.OpenText();
            var code = await sr.ReadToEndAsync();
            var parsedCode = codeParser.Parse(code);
            await using var output = outputPath.CreateText();

            logger.WriteLine($"Compiling [{path}] into [{outputPath}].".Pastel(Color.Aqua));
            compiler.Compile(parsedCode, output, new CompilerConfig
            {
                OptimizerConfig = new OptimizerConfig
                {
                    OptimizationLevel = optimizationLevel
                },
                TypeSafety = typeSafety
            });
        }
        catch (CompilerException exception)
        {
            logger.WriteLine(exception.Message.Pastel(Color.IndianRed));
            outputPath.Delete();
        }
        catch (IOException e)
        {
            logger.WriteLine($"Exception while reading [{path}]:".Pastel(Color.IndianRed));
            logger.WriteLine(e.Message.Pastel(Color.IndianRed));
            return 1;
        }

        if (assemble || link || run)
        {
            if (OperatingSystem.IsWindows())
            {
                outputPath = pathConverter.ConvertToWsl(outputPath);
                objectPath = pathConverter.ConvertToWsl(objectPath);
            }

            logger.WriteLine($"Assembling [{outputPath}] into [{objectPath}].".Pastel(Color.Aqua));
            await executor.AssembleAsync(outputPath, objectPath);
        }

        if (link || run)
        {
            if (OperatingSystem.IsWindows())
            {
                binaryPath = pathConverter.ConvertToWsl(binaryPath);
            }

            logger.WriteLine($"Linking [{objectPath}] into [{binaryPath}].".Pastel(Color.Aqua));
            await executor.LinkAsync(objectPath, binaryPath);
        }

        if (run)
        {
            using var input = inputPath?.OpenText() ?? Console.In;
            logger.WriteLine($"Running [{binaryPath}].".Pastel(Color.Aqua));
            await executor.ExecuteAsync(binaryPath.FullName, "", input);
        }

        logger.WriteLine("Done!".Pastel(Color.Green));
        return 0;
    }

    [GeneratedRegex(@"^(?:[^/\\]*[/\\])*(.*?)(?:\.+[^.]*)?$")]
    private static partial Regex OutputPathRegex();
    [GeneratedRegex(@"\.asm$")]
    private static partial Regex AsmRegex();
}
