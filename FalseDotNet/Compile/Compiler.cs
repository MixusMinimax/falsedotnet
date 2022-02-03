using FalseDotNet.Instructions;
using FalseDotNet.Parse;
using FalseDotNet.Utility;

namespace FalseDotNet.Compile;

public class Compiler : ICompiler
{
    // @formatter:int_align_assignments true
    private static readonly Dictionary<string, string> Macros = new()
    {
        ["SYS_WRITE"]     = "1",
        ["SYS_EXIT"]      = "60",
        ["SYS_MMAP"]      = "9",
        ["PROT_NONE"]     = "0b0000",
        ["PROT_READ"]     = "0b0001",
        ["PROT_WRITE"]    = "0b0010",
        ["PROT_EXEC"]     = "0b1000",
        ["MAP_PRIVATE"]   = "0b00000010",
        ["MAP_ANONYMOUS"] = "0b00100000"
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

    public void Compile(Program program, TextWriter output)
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

        WriteDecimalConverter(output);
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
        O(@"    mov r10, MAP_PRIVATE | MAP_ANONYMOUS");
        O(@"    mov r8, -1");
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
            // Literals

            case Operation.IntLiteral:
                O($"    mov rax, 0x{argument:x8}");
                Push(output, "rax");
                break;

            // Stack

            // Arithmetic

            case Operation.Add:
                Pop(output, "rax");
                Pop(output, "rdx");
                O("    add rax, rdx");
                Push(output, "rax");
                break;

            case Operation.Div:
                Pop(output, "rbx");
                Pop(output, "rax");
                O("    xor rdx, rdx");
                O("    idiv rbx");
                Push(output, "rax");
                break;
            
            case Operation.Neg:
                O("    mov rbx, [rel stack]");
                O("    mov rcx, [rel stack_ptr]");
                O("    dec rcx");
                O("    mov rax, [rbx,rcx*8]");
                O("    neg rax");
                O("    mov [rbx,rcx*8], rax");
                break;

            // I/O

            case Operation.PrintString:
                PrintString(output, argument);
                break;

            case Operation.OutputChar:
                // In the future, write to a buffer instead, and use the flush command 'ß' to write to stdout.
                // A syscall for every character is not efficient.
                Pop(output, "rax");
                O("    push rax");
                O("    mov rax, SYS_WRITE");
                O("    mov rdi, 1");
                O("    mov rsi, rsp");
                O("    mov rdx, 1");
                O("    syscall");
                O("    add rsp, 8");
                break;
            
            case Operation.OutputDecimal:
                Pop(output, "rdi");
                O("    call print_decimal");
                break;

            case Operation.Exit:
                Exit(output, 0); // maybe exit with top of FALSE stack?
                break;
        }
    }

    private void WriteDecimalConverter(TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        O("");
        O("; Converts rdi to decimal and writes to stdout.");
        O("print_decimal:");

        // rax: number, rsi: isNegative, rbx: string base, rcx: string index
        // rdx: modulo

        // 1. take the absolute and remember if number was negative.
        O("    mov rax, rdi");
        O("    neg rdi");
        O("    mov r11, 0");
        O("    mov rdx, -1");
        O("    cmp rax, 0");
        O("    cmovl r11, rdx");
        O("    cmovl rax, rdi");

        // 2. Convert to decimal and store in string_buffer. (right to left)
        //    Count decimal places.
        O("    mov rdi, 10");
        O("    lea r8, [rel string_buffer]");
        O($"    mov rcx, 0x{_config.StringBufferSize:x8}"); // Start at the end
        O("print_decimal_loop:");
        O("    dec rcx");
        O("    xor rdx, rdx");
        O("    div rdi"); // digit in rdx
        O("    add dl, '0'");
        O("    mov [r8,rcx], dl");
        O("    cmp rax, 0");
        O("    jne print_decimal_loop");

        // 3. Write '-' in front if number was negative.
        //    also, increment length counter
        O("    cmp r11, 0");
        O("    je print_decimal_skip");
        O("    dec rcx");
        O("    mov dl, '-'");
        O("    mov [r8,rcx], dl");
        O("print_decimal_skip:");
        
        // 4. Pass string_buffer+32-length as pointer, length to write syscall.
        O("    add r8, rcx");
        O($"    mov rdx, 0x{_config.StringBufferSize:x8}");
        O("    sub rdx, rcx");
        O("    mov rax, SYS_WRITE");
        O("    mov rdi, 1");
        O("    mov rsi, r8");
        O("    syscall");
        O("    ret");
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

    private static void Push(TextWriter output, string register)
    {
        void O(string s) => output.WriteLine(s);
        O(@"    mov rbx, [rel stack]");
        O(@"    mov rcx, [rel stack_ptr]");
        O($"    mov [rbx,rcx*8], {register}");
        O(@"    inc rcx");
        O(@"    mov [rel stack_ptr], rcx");
    }

    private static void Pop(TextWriter output, string register)
    {
        void O(string s) => output.WriteLine(s);
        O(@"    mov rbx, [rel stack]");
        O(@"    mov rcx, [rel stack_ptr]");
        O(@"    dec rcx");
        O($"    mov {register}, [rbx,rcx*8]");
        O(@"    mov [rel stack_ptr], rcx");
    }

    private static void Peek(TextWriter output, string register)
    {
        void O(string s) => output.WriteLine(s);
        // TODO
    }

    /*****************************************\
     *             Data Sections             *
     *****************************************/

    private static void WriteBss(TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        O("; Uninitialized Globals:");
        O("    section .bss");
        O("stack: RESQ 1");
        O("string_buffer: RESB 32");
    }

    private static void WriteData(TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        O("; Globals:");
        O("    section .data");
        O("stack_ptr: DQ 0");
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