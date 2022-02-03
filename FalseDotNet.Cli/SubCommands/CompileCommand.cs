using System.Drawing;
using System.Text.RegularExpressions;
using FalseDotNet.Binary;
using FalseDotNet.Cli.SubCommandExtensions;
using FalseDotNet.Compile;
using FalseDotNet.Parse;
using FalseDotNet.Utility;
using Microsoft.Extensions.DependencyInjection;
using Pastel;

namespace FalseDotNet.Cli.SubCommands;

public class CompileCommand : ISubCommand<CompileOptions>
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

    public static IServiceCollection RegisterServices(IServiceCollection services)
    {
        services.AddSingleton(_ => new CompilerConfig
        {
            StartLabels = new List<string> { "_start", "main" },
            WriteInstructionComments = true,
            StackSize = 65_536,
            StringBufferSize = 32,
        });
        return services;
    }

    public int Run(CompileOptions opts)
    {
        if (string.IsNullOrWhiteSpace(opts.OutputPath))
            opts.OutputPath = Regex.Replace(opts.InputPath, @"^(?:[^/\\]*[/\\])*(.*?)(?:\.+[^.]*)?$", "$1.asm");
        if (Path.GetFullPath(opts.InputPath) == Path.GetFullPath(opts.OutputPath))
            throw new ArgumentException("Input and Output path point to the same file!");
        new FileInfo(opts.OutputPath).Directory?.Create();

        var objectPath = Regex.Replace(opts.OutputPath, @"\.asm$", ".o");
        var binaryPath = Regex.Replace(opts.OutputPath, @"\.asm$", "");

        try
        {
            using var sr = new StreamReader(opts.InputPath);
            var code = sr.ReadToEnd();
            var parsedCode = _codeParser.Parse(code);
            using var output = new StreamWriter(opts.OutputPath);

            _logger.WriteLine($"Compiling [{opts.InputPath}] into [{opts.OutputPath}].".Pastel(Color.Aqua));
            _compiler.Compile(parsedCode, output);
        }
        catch (CompilerException exception)
        {
            _logger.WriteLine(exception.Message.Pastel(Color.IndianRed));
            File.Delete(opts.OutputPath);
        }
        catch (IOException e)
        {
            _logger.WriteLine($"Exception while reading [{opts.InputPath}]:".Pastel(Color.IndianRed));
            _logger.WriteLine(e.Message.Pastel(Color.IndianRed));
            return 1;
        }

        if (opts.Assemble)
        {
            _logger.WriteLine($"Assembling [{opts.OutputPath}] into [{objectPath}].".Pastel(Color.Aqua));
            _executor.Assemble(opts.OutputPath, objectPath);
        }

        if (opts.Link)
        {
            _logger.WriteLine($"Linking [{objectPath}] into [{binaryPath}].".Pastel(Color.Aqua));
            _executor.Link(objectPath, binaryPath);
        }

        return 0;
    }
}