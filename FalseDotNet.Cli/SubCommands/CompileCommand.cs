using System.Drawing;
using System.Text.RegularExpressions;
using FalseDotNet.Binary;
using FalseDotNet.Cli.SubCommandExtensions;
using FalseDotNet.Compile;
using FalseDotNet.Compile.Optimization;
using FalseDotNet.Parse;
using FalseDotNet.Utility;
using Pastel;

namespace FalseDotNet.Cli.SubCommands;

public class CompileCommand : SubCommand<CompileOptions>
{
    private readonly ILogger _logger;
    private readonly ICodeParser _codeParser;
    private readonly ICompiler _compiler;
    private readonly ILinuxExecutor _executor;

    public CompileCommand(ILogger logger, ICodeParser codeParser, ICompiler compiler, ILinuxExecutor executor)
    {
        _logger = logger;
        _codeParser = codeParser;
        _compiler = compiler;
        _executor = executor;
    }

    private CompilerConfig MapOptionsToCompilerConfig(CompileOptions options)
    {
        return new CompilerConfig
        {
            OptimizerConfig =
            {
                OptimizationLevel = options.OptimizationLevel switch
                {
                    0 => OptimizerConfig.EOptimizationLevel.O0,
                    1 => OptimizerConfig.EOptimizationLevel.O1,
                    2 => OptimizerConfig.EOptimizationLevel.O2,
                    _ => throw new ArgumentException("Optimization level must be one of O0, O1, O2!")
                }
            }
        };
    }

    public override int Run(CompileOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.OutputPath))
            options.OutputPath = Regex.Replace(options.InputPath, @"^(?:[^/\\]*[/\\])*(.*?)(?:\.+[^.]*)?$", "$1.asm");
        if (Path.GetFullPath(options.InputPath) == Path.GetFullPath(options.OutputPath))
            throw new ArgumentException("Input and Output path point to the same file!");
        new FileInfo(options.OutputPath).Directory?.Create();

        var objectPath = Regex.Replace(options.OutputPath, @"\.asm$", ".o");
        var binaryPath = Regex.Replace(options.OutputPath, @"\.asm$", "");

        try
        {
            using var sr = new StreamReader(options.InputPath);
            var code = sr.ReadToEnd();
            var parsedCode = _codeParser.Parse(code);
            using var output = new StreamWriter(options.OutputPath);

            _logger.WriteLine($"Compiling [{options.InputPath}] into [{options.OutputPath}].".Pastel(Color.Aqua));
            _compiler.Compile(parsedCode, output, MapOptionsToCompilerConfig(options));
        }
        catch (CompilerException exception)
        {
            _logger.WriteLine(exception.Message.Pastel(Color.IndianRed));
            File.Delete(options.OutputPath);
        }
        catch (IOException e)
        {
            _logger.WriteLine($"Exception while reading [{options.InputPath}]:".Pastel(Color.IndianRed));
            _logger.WriteLine(e.Message.Pastel(Color.IndianRed));
            return 1;
        }

        if (options.Assemble)
        {
            _logger.WriteLine($"Assembling [{options.OutputPath}] into [{objectPath}].".Pastel(Color.Aqua));
            _executor.Assemble(options.OutputPath, objectPath);
        }

        if (options.Link)
        {
            _logger.WriteLine($"Linking [{objectPath}] into [{binaryPath}].".Pastel(Color.Aqua));
            _executor.Link(objectPath, binaryPath);
        }

        _logger.WriteLine("Done!".Pastel(Color.Green));
        return 0;
    }
}