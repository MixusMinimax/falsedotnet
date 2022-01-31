using CommandLine;

namespace FalseDotNet.Cli.SubCommands;

[Verb("interpret", HelpText = "interpret FALSE code.")]
public class InterpretOptions
{
    [Value(0, MetaName = "PATH", Required = true, HelpText = "File containing FALSE code.")]
    public string InputPath { get; set; } = default!;
}