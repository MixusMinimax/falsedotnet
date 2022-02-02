using FalseDotNet.Operations;
using FalseDotNet.Parsing;
using FalseDotNet.Utility;

namespace FalseDotNet.Compilation;

public class Compiler : ICompiler
{
    // @formatter:int_align_assignments true
    private static readonly Dictionary<string, string> Macros = new()
    {
        ["SYS_WRITE"]  = "1",
        ["SYS_EXIT"]   = "60",
        ["SYS_MMAP"]   = "9",
        ["PROT_NONE"]  = "0b0000",
        ["PROT_READ"]  = "0b0001",
        ["PROT_WRITE"] = "0b0010",
        ["PROT_EXEC"]  = "0b1000",
    };

    private static readonly Dictionary<string, string> Strings = new()
    {
        ["mmap_error"] = "MMAP Failed! Exiting.\n",
    };
    // @formatter:int_align_assignments restore

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
        output.WriteLine('\n');
        WriteConstants(output);
        output.WriteLine('\n');
        WriteText(program, output);
        output.WriteLine('\n');
        WriteBss(output);
        output.WriteLine('\n');
        WriteData(output);
        output.WriteLine('\n');
        WriteRoData(program, output);
    }

    private string GetLabel(Program program, long id) =>
        id == program.EntryId ? _config.StartLabels.First() : $"_lambda_{id:D3}";

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
        foreach (var (key, value) in Macros)
        {
            output.WriteLine($"%define {key} {value}");
        }
    }

    private void WriteText(Program program, TextWriter output)
    {
        output.WriteLine("; Code:");
        output.WriteLine("    section .text");
        foreach (var label in _config.StartLabels)
            output.WriteLine($"    global {label}");

        foreach (var id in program.Functions.Keys)
        {
            CompileLambda(program, output, id);
        }
    }

    private void WriteSetup(TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        O(@";===[SETUP START]===");
        O(@"    ; Allocate FALSE stack:");
        O(@"    mov rax, SYS_MMAP");
        O(@"    mov rdi, 0");
        O($"    mov rsi, 0x{_config.StackSize:x8}");
        O(@"    mov rdx, PROT_READ | PROT_WRITE");
        O(@"    mov rcx, 0");
        O(@"    mov r8, 0");
        O(@"    mov r9, 0");
        O(@"    syscall");
        O(@"    ; Check for mmap success");
        O(@"    cmp rax, -1");
        O(@"    jne skip_mmap_error");
        O(@"    ; Print the fact that mmap failed");
        PrintString(output, "mmap_error", "mmap_error_len", 2);
        Exit(output, 1);
        O(@"skip_mmap_error:");
        O(@"    mov [rel stack], rax");
        O(@";===[SETUP  END]===");
        O("");
    }

    private void CompileLambda(Program program, TextWriter output, long lambdaId)
    {
        output.WriteLine();
        if (lambdaId == program.EntryId)
        {
            foreach (var label in _config.StartLabels)
                output.WriteLine($"{label}:");
            WriteSetup(output);
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
                PrintString(output, argument);
                break;

            case Operation.Exit:
                Exit(output, 0); // maybe exit with top of FALSE stack?
                break;
        }
    }

    /*****************************************\
     *            Common Patterns            *
     *****************************************/

    private static void PrintString(TextWriter output, long id, int fd = 1)
    {
        PrintString(output, GetStringLabel(id), GetStringLenLabel(id), fd);
    }

    private static void PrintString(TextWriter output, string strLabel, string lenLabel, int fd = 1)
    {
        void O(string s) => output.WriteLine(s);
        O(@"    mov rax, SYS_WRITE");
        O($"    mov rdi, {fd}"); // stdout
        O($"    lea rsi, [rel {strLabel}]");
        O($"    mov rdx, {lenLabel}");
        O(@"    syscall");
    }

    private static void Exit(TextWriter output, int exitCode = 0)
    {
        Exit(output, $"{exitCode}");
    }

    private static void Exit(TextWriter output, string exitCode)
    {
        void O(string s) => output.WriteLine(s);
        O(@"    mov rax, SYS_EXIT");
        O($"    mov rdi, {exitCode}");
        O(@"    syscall");
    }

    /*****************************************\
     *             Data Sections             *
     *****************************************/

    private static void WriteBss(TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        O("; Uninitialized Globals:");
        O("    section .bss");
        O("stack: resq 1");
    }

    private static void WriteData(TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        O("; Globals:");
        O("    section .data");
    }

    private static void WriteRoData(Program program, TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        O("; Constants:");
        O("    section .rodata");
        foreach (var entry in program.Strings)
        {
            var (key, value) = entry;
            O($"{GetStringLabel(key)}: DB {value.Escape('`')}");
            O($"{GetStringLenLabel(key)}: EQU $ - {GetStringLabel(key)}");
        }

        O("");
        O("; Constant Constants:");
        foreach (var entry in Strings)
        {
            var (key, value) = entry;
            O($"{key}: DB {value.Escape('`')}");
            O($"{key}_len: EQU $ - {key}");
        }
    }
}