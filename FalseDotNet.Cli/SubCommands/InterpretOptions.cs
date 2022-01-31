using CommandLine;

namespace FalseDotNet.Cli.SubCommands;

[Verb("interpret", HelpText = "interpret FALSE code.")]
public class InterpretOptions
{
    [Value(0, MetaName = "PATH", Required = true, HelpText = "File containing FALSE code.")]
    public string InputPath { get; set; } = default!;

    [Option('p', "print-operations", Default = false, HelpText = "Print operations before executing them.")]
    public bool PrintOperations { get; set; }
}