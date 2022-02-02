using CommandLine;

namespace FalseDotNet.Cli.SubCommands;

[Verb("compile", HelpText = "compile FALSE code.")]
public class CompileOptions
{
    [Value(0, MetaName = "PATH", Required = true, HelpText = "File containing FALSE code.")]
    public string InputPath { get; set; } = default!;

    [Option('o', "output", MetaValue = "PATH", HelpText = "File path to write assembly to. Defaults to '<input>.asm'.")]
    public string OutputPath { get; set; } = default!;
}