using CommandLine;
using FalseDotNet.Utility;

namespace FalseDotNet.Cli.SubCommands;

[Verb("interpret", HelpText = "interpret FALSE code.")]
public class InterpretOptions
{
    [Value(0, MetaName = "PATH", Required = true, HelpText = "File containing FALSE code.")]
    public string InputPath { get; set; } = default!;

    [Option('p', "print-operations", Default = false, HelpText = "Print operations before executing them.")]
    public bool PrintOperations { get; set; }
    
    [Option('t', "type-safety", Default = TypeSafety.None,
        HelpText =
            "What level of type safety to enforce.\n" +
            "LAMBDA only enforces lambda execution, but allows integers\n" +
            "to work as references, since they are masked anyway.")]
    public TypeSafety TypeSafety { get; set; }
}