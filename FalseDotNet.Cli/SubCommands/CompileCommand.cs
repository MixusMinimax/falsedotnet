using FalseDotNet.Cli.ParserExtensions;

namespace FalseDotNet.Cli.SubCommands;

public class CompileCommand: ISubCommand<CompileOptions>
{
    public int Run(CompileOptions opts)
    {
        Console.WriteLine($"Compile. Input file: {opts.InputPath}");
        return 0;
    }
}