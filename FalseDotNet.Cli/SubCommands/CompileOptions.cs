using CommandLine;
using FalseDotNet.Utility;

namespace FalseDotNet.Cli.SubCommands;

[Verb("compile", HelpText = "compile FALSE code.")]
public class CompileOptions
{
    [Value(0, MetaName = "PATH", Required = true, HelpText = "File containing FALSE code.")]
    public string InputPath { get; set; } = default!;

    [Option('i', "input", HelpText = "Read from file instead of stdin for program input.")]
    public string? StdinPath { get; set; }
    
    [Option('o', "output", MetaValue = "PATH", HelpText = "File path to write assembly to. Defaults to '<input>.asm'.")]
    public string OutputPath { get; set; } = default!;

    [Option('a', "assemble", HelpText = "Assemble using nasm.")]
    public bool Assemble { get; set; }

    [Option('l', "link", HelpText = "Link using ld.")]
    public bool Link { get; set; }

    [Option('r', "run", HelpText = "Run after compilation.")]
    public bool Run { get; set; }

    [Option('O', "optimization", HelpText = "Level of optimization: O0, O1, O2.")]
    public uint OptimizationLevel { get; set; }

    [Option('t', "type-safety", Default = TypeSafety.None,
        HelpText =
            "What level of type safety to enforce.\n" +
            "LAMBDA only enforces lambda execution, but allows integers\n" +
            "to work as references, since they are masked anyway.")]
    public TypeSafety TypeSafety { get; set; }
}