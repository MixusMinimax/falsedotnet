using System.Drawing;
using CommandLine;
using FalseDotNet.Cli.ParserExtensions;
using Pastel;

namespace FalseDotNet.Cli.SubCommands;

public class InterpretCommand : ISubCommand<InterpretOptions>
{
    private ICodeParser _codeParser;
    private ILogger _logger;

    public InterpretCommand(ICodeParser codeParser, ILogger logger)
    {
        _codeParser = codeParser;
        _logger = logger;
    }

    public int Run(InterpretOptions opts)
    {
        _logger.WriteLine($"Interpreting [{opts.InputPath}].".Pastel(Color.Aqua));
        try
        {
            using var sr = new StreamReader(opts.InputPath);
            var code = sr.ReadToEnd();
            var parsedCode = _codeParser.Parse(code);
        }
        catch (IOException e)
        {
            _logger.WriteLine($"Exception while reading [{opts.InputPath}]:".Pastel(Color.IndianRed));
            _logger.WriteLine(e.Message.Pastel(Color.IndianRed));
            return 1;
        }

        return 0;
    }
}