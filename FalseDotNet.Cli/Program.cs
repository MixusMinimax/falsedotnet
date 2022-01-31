using CommandLine;

int RunInterpretAndReturnExitCode(InterpreterOptions opts)
{
    Console.WriteLine($"Interpret. Input file: {opts.InputPath}");
    return 0;
}

int RunCompileAndReturnExitCode(CompileOptions opts)
{
    Console.WriteLine($"Compile. Input file: {opts.InputPath}");
    return 0;
}

return Parser.Default.ParseArguments<InterpreterOptions, CompileOptions>(args).MapResult(
    (InterpreterOptions opts) => RunInterpretAndReturnExitCode(opts),
    (CompileOptions opts) => RunCompileAndReturnExitCode(opts),
    errs => 1
);

[Verb("interpret", HelpText = "interpret FALSE code.")]
class InterpreterOptions
{
    [Value(0, MetaName = "PATH", Required = true, HelpText = "File containing FALSE code.")]
    public string InputPath { get; set; } = default!;
}

[Verb("compile", HelpText = "compile FALSE code.")]
class CompileOptions
{
    [Value(0, MetaName = "PATH", Required = true, HelpText = "File containing FALSE code.")]
    public string InputPath { get; set; } = default!;
}