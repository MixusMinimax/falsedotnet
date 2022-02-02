using FalseDotNet.Operations;
using FalseDotNet.Parsing;
using FalseDotNet.Utility;

namespace FalseDotNet.Compilation;

public class Compiler : ICompiler
{
    private readonly ILogger _logger;
    private readonly CompilerConfig _config;

    public Compiler(ILogger logger, CompilerConfig config)
    {
        _logger = logger;
        _config = config;
    }

    public void Compile(Program program, StreamWriter output)
    {
        WriteHeader(output);
        output.WriteLine();
        WriteConstants(output);
        output.WriteLine();
        WriteText(program, output);
        output.WriteLine();
        WriteRoData(program, output);
        // TODO: Create stack in [.data]. Using the heap might also make sense, however, this will be fine for now.
    }

    private string GetLabel(Program program, long id) =>
        id == program.EntryId ? _config.StartLabels.First() : $"_label_{id:D3}";

    private static string GetStringLabel(long id) =>
        $"str_{id:D3}";

    private static string GetStringLenLabel(long id) =>
        $"len_{id:D3}";

    private static void WriteHeader(TextWriter output)
    {
        output.WriteLine("; Generated using the FALSE.NET compiler.");
        output.WriteLine("; =======================================");
    }

    private static void WriteConstants(TextWriter output)
    {
        output.WriteLine("; Constants:");
        output.WriteLine("%define SYS_WRITE 1");
        output.WriteLine("%define SYS_EXIT 60");
    }

    private void WriteText(Program program, TextWriter output)
    {
        output.WriteLine("    section .text");
        foreach (var label in _config.StartLabels)
            output.WriteLine($"    global {label}");

        foreach (var id in program.Functions.Keys)
        {
            CompileLambda(program, output, id);
        }
    }

    private void CompileLambda(Program program, TextWriter output, long lambdaId)
    {
        if (lambdaId == program.EntryId)
        {
            foreach (var label in _config.StartLabels)
                output.WriteLine($"{label}:");
        }
        else
        {
            output.WriteLine($"{GetLabel(program, lambdaId)}:");
        }

        var lambda = program.Functions[lambdaId];
        foreach (var instruction in lambda)
        {
            CompileInstruction(instruction, output);
        }
    }

    private void CompileInstruction(Instruction instruction, TextWriter output)
    {
        void O(string s) => output.WriteLine(s);

        if (_config.WriteInstructionComments)
            O($"    ; -- {instruction} --");
        var (operation, argument) = instruction;
        switch (operation)
        {
            case Operation.PrintString:
                O(@"    mov rax, SYS_WRITE");
                O(@"    mov rdi, 1");
                O($"    lea rsi, [rel {GetStringLabel(argument)}]");
                O($"    mov rdx, {GetStringLenLabel(argument)}");
                O(@"    syscall");
                break;
            
            case Operation.Exit:
                O(@"    mov rax, SYS_EXIT");
                O(@"    mov rdi, 0"); // maybe exit with top of FALSE stack?
                O(@"    syscall");
                break;
        }
    }

    private void WriteRoData(Program program, TextWriter output)
    {
        output.WriteLine("    section .rodata");
        foreach (var entry in program.Strings)
        {
            var (key, value) = entry;
            output.WriteLine($"{GetStringLabel(key)}: DB {value.Escape('`')}");
            output.WriteLine($"{GetStringLenLabel(key)}: EQU $ - {GetStringLabel(key)}");
        }
    }
}