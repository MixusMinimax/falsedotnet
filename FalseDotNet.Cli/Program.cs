using System.CommandLine;
using System.CommandLine.Parsing;
using FalseDotNet.Cli.SubCommands;
using FalseDotNet.Compile.Optimization;
using FalseDotNet.Utility;

namespace FalseDotNet.Cli;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var inputPathOption = new Option<FileInfo?>(
            aliases: ["--input", "-i"],
            description: "Read from file instead of stdin for program input.",
            isDefault: true,
            parseArgument: ParseFileInfo
        );

        var typeSafetyOption = new Option<TypeSafety>(
            aliases: ["--type-safety", "-t"],
            description: "What level of type safety to enforce.\n"
                         + "LAMBDA only enforces lambda execution, but allows integers\n"
                         + "to work as references, since they are masked anyway.",
            getDefaultValue: () => TypeSafety.None
        );

        var printOperationsOption = new Option<bool>(
            aliases: ["--print-operations", "-p"],
            description: "Print operations before executing them."
        );

        // positional
        var pathArgument = new Argument<FileInfo>(
            name: "PATH",
            description: "File containing FALSE code.",
            parse: result => ParseFileInfo(result)!
        );

        var runCommand = new Command("run", "Interpret a False program")
        {
            inputPathOption,
            typeSafetyOption,
            printOperationsOption,
            pathArgument
        };
        runCommand.Handler = new RunCommandHandler(
            inputPathOption, typeSafetyOption, printOperationsOption, pathArgument
        );

        var outputPathOption = new Option<FileInfo?>(
            aliases: ["--output", "-o"],
            description: "File path to write assembly to. Defaults to '<input>.asm'.",
            parseArgument: ParseFileInfo
        );

        var optimizationLevelOption = new Option<OptimizerConfig.EOptimizationLevel>(
            aliases: ["--optimization", "-O"],
            description: "Level of optimization: O0, O1, O2.",
            parseArgument: result =>
            {
                var level = result.Tokens.FirstOrDefault();
                if (level is null)
                    return OptimizerConfig.EOptimizationLevel.O0;
                switch (level.Value.ToLower())
                {
                    case "o0" or "0":
                        return OptimizerConfig.EOptimizationLevel.O0;
                    case "o1" or "1":
                        return OptimizerConfig.EOptimizationLevel.O1;
                    case "o2" or "2":
                        return OptimizerConfig.EOptimizationLevel.O2;
                    default:
                        result.ErrorMessage = "Invalid optimization level.";
                        return default;
                }
            }
        );

        var assembleOption = new Option<bool>(
            aliases: ["--assemble", "-a"],
            description: "Assemble using nasm."
        );

        var linkOption = new Option<bool>(
            aliases: ["--link", "-l"],
            description: "Link using ld."
        );

        var runOption = new Option<bool>(
            aliases: ["--run", "-r"],
            description: "Run after compilation."
        );

        var compileCommand = new Command("compile", "Compile a False program")
        {
            inputPathOption,
            outputPathOption,
            typeSafetyOption,
            optimizationLevelOption,
            assembleOption,
            linkOption,
            runOption,
            pathArgument
        };
        compileCommand.Handler = new CompileCommandHandler(
            inputPathOption: inputPathOption,
            outputPathOption: outputPathOption,
            typeSafetyOption: typeSafetyOption,
            optimizationLevelOption: optimizationLevelOption,
            assembleOption: assembleOption,
            linkOption: linkOption,
            runOption: runOption,
            pathArgument: pathArgument
        );

        var rootCommand = new RootCommand("FalseDotNet CLI") { runCommand, compileCommand };

        return await rootCommand.InvokeAsync(args);
    }

    private static FileInfo? ParseFileInfo(SymbolResult result)
    {
        var pathString = result.Tokens.FirstOrDefault();
        if (pathString is null)
            return null;
        var path = new FileInfo(pathString.Value);
        if (path.Exists)
            return path;
        result.ErrorMessage = $"File {path.FullName} does not exist.";
        return null;
    }
}
