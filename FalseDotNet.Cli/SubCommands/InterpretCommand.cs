using System.Drawing;
using FalseDotNet.Cli.SubCommandExtensions;
using FalseDotNet.Interpret;
using FalseDotNet.Parse;
using FalseDotNet.Utility;
using Pastel;

namespace FalseDotNet.Cli.SubCommands;

public class InterpretCommand : ISubCommand<InterpretOptions>
{
    private readonly ILogger _logger;
    private readonly ICodeParser _codeParser;
    private readonly IInterpreter _interpreter;

    public InterpretCommand(ILogger logger, ICodeParser codeParser, IInterpreter interpreter)
    {
        _logger = logger;
        _codeParser = codeParser;
        _interpreter = interpreter;
    }

    public int Run(InterpretOptions opts)
    {
        _logger.WriteLine($"Interpreting [{opts.InputPath}].".Pastel(Color.Aqua));
        try
        {
            using var sr = new StreamReader(opts.InputPath);
            var code = sr.ReadToEnd();
            var parsedCode = _codeParser.Parse(code);
            _interpreter.Interpret(parsedCode, opts.PrintOperations);
        }
        catch (InterpreterException exception)
        {
            _logger.WriteLine(exception.Message.Pastel(Color.IndianRed));
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