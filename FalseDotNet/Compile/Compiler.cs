﻿using FalseDotNet.Commands;
using FalseDotNet.Compile.Optimization;
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
    private readonly IIdGenerator _idGenerator;
    private readonly IOptimizer _optimizer;
    private CompilerConfig _config = null!;

    public Compiler(ILogger logger, IIdGenerator idGenerator, IOptimizer optimizer)
    {
        _logger = logger;
        _idGenerator = idGenerator;
        _optimizer = optimizer;
    }

    public void Compile(Program program, TextWriter output, CompilerConfig config)
    {
        _config = config;
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

        _optimizer.Optimize(new Assembly(), config.OptimizerConfig);
    }

    private string GetLabel(Program program, long id) =>
        id == program.EntryId ? _config.StartLabels.First() : $"_lambda_{id:D3}";

    private static string GetStringLabel(long id) =>
        $"str_{id:D3}";

    private static string GetStringLenLabel(long id) =>
        $"len_{id:D3}";

    private string GenerateNewLabel() =>
        $"_local_{_idGenerator.NewId:D3}";

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
        WritePrintCharacter(output);
        WriteFlushStdout(output);
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
        foreach (var command in lambda)
        {
            CompileCommand(program, command, output);
        }
    }

    private void CompileCommand(Program program, Command command, TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        var (operation, argument) = command;
        if (operation is Operation.WhileCondition or Operation.WhileBody)
            return;
        if (_config.WriteCommandComments)
            O($"    ; -- {command} --");

        switch (operation)
        {
            // Literals

            case Operation.IntLiteral:
                O($"    mov rax, 0x{argument:x8}");
                Push(output, "rax");
                break;

            // Stack

            case Operation.Dup:
                Peek(output, "rax");
                Push(output, "rax");
                break;

            case Operation.Drop:
                Drop(output);
                break;

            case Operation.Swap:
                O(@"    mov rbx, [rel stack]");
                O(@"    mov rcx, [rel stack_ptr]");
                O(@"    mov rax, [rbx+(rcx-1)*8]");
                O(@"    mov rdx, [rbx+(rcx-2)*8]");
                O(@"    mov [rbx+(rcx-1)*8], rdx");
                O(@"    mov [rbx+(rcx-2)*8], rax");
                break;

            case Operation.Rot:
                O(@"    mov rbx, [rel stack]");
                O(@"    mov rcx, [rel stack_ptr]");
                O(@"    mov rax, [rbx+(rcx-1)*8]");
                O(@"    mov rdx, [rbx+(rcx-2)*8]");
                O(@"    mov rsi, [rbx+(rcx-3)*8]");
                O(@"    mov [rbx+(rcx-1)*8], rsi");
                O(@"    mov [rbx+(rcx-2)*8], rax");
                O(@"    mov [rbx+(rcx-3)*8], rdx");
                break;

            case Operation.Pick:
                Peek(output, "rax"); // Offset
                O("    mov rsi, rcx");
                O("    sub rsi, rax");
                O("    mov rax, [rbx+(rsi-2)*8]");
                O("    mov [rbx+(rcx-1)*8], rax");
                break;


            // Arithmetic

            case Operation.Add:
                Pop(output, "rax");
                O(@"    mov rdx, [rbx+(rcx-1)*8]");
                O("    add rax, rdx");
                O("    mov [rbx+(rcx-1)*8], rax");
                break;

            case Operation.Sub:
                Pop(output, "rax");
                O(@"    mov rdx, [rbx+(rcx-1)*8]");
                O("    sub rdx, rax");
                O("    mov [rbx+(rcx-1)*8], rdx");
                break;

            case Operation.Mul:
                Pop(output, "rax");
                O(@"    mov rdx, [rbx+(rcx-1)*8]");
                O("    imul rax, rdx");
                O("    mov [rbx+(rcx-1)*8], rax");
                break;

            case Operation.Div:
                Pop(output, "rdi");
                O(@"    mov rax, [rbx+(rcx-1)*8]");
                O("    xor rdx, rdx");
                O("    mov rsi, rdx");
                O("    not rsi");
                O("    cmp rax, 0");
                O("    cmovl rdx, rsi");
                O("    idiv rdi");
                O("    mov [rbx+(rcx-1)*8], rax");
                break;

            case Operation.Neg:
                Peek(output, "rax");
                O("    neg rax");
                O("    mov [rbx+(rcx-1)*8], rax");
                break;

            case Operation.And:
                Pop(output, "rax");
                O(@"    mov rdx, [rbx+(rcx-1)*8]");
                O("    and rax, rdx");
                O("    mov [rbx+(rcx-1)*8], rax");
                break;

            case Operation.Or:
                Pop(output, "rax");
                O(@"    mov rdx, [rbx+(rcx-1)*8]");
                O("    or rax, rdx");
                O("    mov [rbx+(rcx-1)*8], rax");
                break;

            case Operation.Not:
                Peek(output, "rax");
                O("    not rax");
                O("    mov [rbx+(rcx-1)*8], rax");
                break;

            // Comparison

            case Operation.Eq:
                Pop(output, "rax");
                O(@"    mov rdx, [rbx+(rcx-1)*8]");
                O("    cmp rax, rdx");
                O("    mov rax, 0");
                O("    mov rdx, 0xffffffffffffffff");
                O("    cmove rax, rdx");
                O("    mov [rbx+(rcx-1)*8], rax");
                break;

            case Operation.Gt:
                Pop(output, "rax");
                O(@"    mov rdx, [rbx+(rcx-1)*8]");
                O("    cmp rax, rdx");
                O("    mov rax, 0");
                O("    mov rdx, 0xffffffffffffffff");
                O("    cmovl rax, rdx");
                O("    mov [rbx+(rcx-1)*8], rax");
                break;

            // Control Flow and Lambdas

            case Operation.Lambda:
                O($"    lea rax, [rel {GetLabel(program, argument)}]");
                Push(output, "rax");
                break;

            case Operation.Ret:
                O("    ret");
                break;

            case Operation.Execute:
                Pop(output, "rax");
                O($"    call rax");
                break;

            case Operation.ConditionalExecute:
                var label = GenerateNewLabel();
                Pop(output, "rax"); // body
                Pop(output, "rdx"); // condition
                O(@"    cmp rdx, 0");
                O($"    jz {label}");
                O(@"    call rax");
                O($"{label}:");
                break;

            case Operation.WhileInit:
                Pop(output, "rax"); // body
                O("    push rax");
                Pop(output, "rax"); // condition
                O("    push rax");
                var loop = GenerateNewLabel();
                var condition = GenerateNewLabel();
                O($"    jmp {condition}");
                O($"{loop}:");
                O(@"    mov rax, [rsp+8]");
                O(@"    call rax");
                O($"{condition}:");
                O(@"    mov rax, [rsp]");
                O(@"    call rax");
                Pop(output, "rax");
                O(@"    cmp rax, 0");
                O($"    jnz {loop}");
                O(@"    add rsp, 16");
                break;

            // Names

            case Operation.Ref:
                O($"    mov rax, {argument}");
                Push(output, "rax");
                break;

            case Operation.Store:
                Pop(output, "rax");
                Pop(output, "rdx");
                O("    lea rbx, [rel references]");
                O("    mov [rbx+rax*8], rdx");
                break;

            case Operation.Load:
                Peek(output, "rax");
                O("    lea rbx, [rel references]");
                O("    mov rdx, [rbx+rax*8]");
                Replace(output, "rdx");
                break;

            // I/O

            case Operation.PrintString:
                PrintString(output, argument);
                break;

            case Operation.OutputChar:
                Pop(output, "rdi");
                O("    call print_character");
                break;

            case Operation.OutputDecimal:
                Pop(output, "rdi");
                O("    call print_decimal");
                break;

            case Operation.Flush:
                O("    call flush_stdout");
                break;

            case Operation.Exit:
                Exit(output, 0); // maybe exit with top of FALSE stack?
                break;

            case Operation.WhileCondition or Operation.WhileBody:
                throw new CompilerException("Unreachable");
        }
    }

    private void WriteDecimalConverter(TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        O("");
        O("; Converts rdi to decimal and writes to stdout.");
        O("print_decimal:");

        // For now, flush stdout. In the future, this function will also write into the buffer.
        O("    push rdi");
        O("    call flush_stdout");
        O("    pop rdi");

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

    private void WritePrintCharacter(TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        O("");
        O("; Prints character located in dil.");
        O("print_character:");
        O("    lea rsi, [rel stdout_buffer]");
        O("    xor rdx, rdx");
        O("    mov dx, [rel stdout_len]");
        O("    mov [rsi+rdx], rdi");
        O("    inc dx");
        O("    mov r8b, 0");
        O("    mov r9b, 0");
        O("    mov r10b, 0xff");
        O($"    cmp dx, {_config.StdoutBufferSize}");
        O("    cmove r8, r10");
        O("    cmp dil, 10"); // flush on newlines
        O("    cmove r9, r10");
        O("    or r8b, r9b");
        O("    cmp r8b, 0");
        O("    jz print_character_ret");
        O("    mov rax, SYS_WRITE");
        O("    mov rdi, 1");
        // rsi is already pointing to the string
        // rdx is already the length of the string
        O("    syscall");
        O("    mov dx, 0");
        O("print_character_ret:");
        O("    mov [rel stdout_len], dx");
        O("    ret");
    }

    private void WriteFlushStdout(TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        O("");
        O("; Flushes stdout.");
        O("flush_stdout:");
        O("    lea rsi, [rel stdout_buffer]");
        O("    xor rdx, rdx");
        O("    mov dx, [rel stdout_len]");
        O("    cmp dx, 0");
        O("    jz flush_stdout_ret");
        O("    mov rax, SYS_WRITE");
        O("    mov rdi, 1");
        O("    syscall");
        O("    mov dx, 0");
        O("    mov [rel stdout_len], dx");
        O("flush_stdout_ret:");
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
        O(@"    push rdi");
        O(@"    call flush_stdout");
        O(@"    pop rdi");
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

    private static void Drop(TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        O(@"    mov rcx, [rel stack_ptr]");
        O(@"    dec rcx");
        O(@"    mov [rel stack_ptr], rcx");
    }

    private static void Replace(TextWriter output, string register)
    {
        void O(string s) => output.WriteLine(s);
        O(@"    mov rbx, [rel stack]");
        O(@"    mov rcx, [rel stack_ptr]");
        O($"    mov [rbx+(rcx-1)*8], {register}");
    }

    private static void Peek(TextWriter output, string register)
    {
        void O(string s) => output.WriteLine(s);
        O(@"    mov rbx, [rel stack]");
        O(@"    mov rcx, [rel stack_ptr]");
        O($"    mov {register}, [rbx+(rcx-1)*8]");
    }

    /*****************************************\
     *             Data Sections             *
     *****************************************/

    private void WriteBss(TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        O("; Uninitialized Globals:");
        O("    section .bss");
        O("references: RESQ 32");
        O("stack: RESQ 1");
        O("string_buffer: RESB 32");
        O($"stdout_buffer: RESB {_config.StdoutBufferSize}");
    }

    private static void WriteData(TextWriter output)
    {
        void O(string s) => output.WriteLine(s);
        O("; Globals:");
        O("    section .data");
        O("stack_ptr: DQ 0");
        O("stdout_len: DW 0");
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