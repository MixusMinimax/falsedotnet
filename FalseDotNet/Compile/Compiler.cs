using FalseDotNet.Commands;
using FalseDotNet.Compile.Instructions;
using FalseDotNet.Compile.Optimization;
using FalseDotNet.Parse;
using FalseDotNet.Utility;

namespace FalseDotNet.Compile;

public class Compiler : ICompiler
{
    // @formatter:int_align_assignments true
    private static readonly Register Rax = new Register(ERegister.ax, ERegisterSize.r);
    private static readonly Register Rbx = new Register(ERegister.bx, ERegisterSize.r);
    private static readonly Register Rcx = new Register(ERegister.cx, ERegisterSize.r);
    private static readonly Register Rdx = new Register(ERegister.dx, ERegisterSize.r);
    private static readonly Register Rdi = new Register(ERegister.di, ERegisterSize.r);
    private static readonly Register Rsi = new Register(ERegister.si, ERegisterSize.r);
    private static readonly Register R8 = new Register(ERegister.r8, ERegisterSize.r);
    private static readonly Register R9 = new Register(ERegister.r9, ERegisterSize.r);
    private static readonly Register R10 = new Register(ERegister.r10, ERegisterSize.r);

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
    private Asm _asm = null!;

    public Compiler(ILogger logger, IIdGenerator idGenerator, IOptimizer optimizer)
    {
        _logger = logger;
        _idGenerator = idGenerator;
        _optimizer = optimizer;
    }

    public void Compile(Program program, TextWriter output, CompilerConfig config)
    {
        _config = config;
        _asm = new Asm();

        WriteHeader();
        _asm.Str();
        WriteConstants();
        _asm.Str();
        WriteText(program);
        _asm.Str();
        WriteBss();
        _asm.Str();
        WriteData();
        _asm.Str();
        WriteRoData(program);

        _optimizer.Optimize(_asm, config.OptimizerConfig);

        _asm.WriteOut(output);
    }

    private string GetLabel(Program program, long id) =>
        id == program.EntryId ? _config.StartLabels.First() : $"_lambda_{id:D3}";

    private static string GetStringLabel(long id) =>
        $"str_{id:D3}";

    private static string GetStringLenLabel(long id) =>
        $"len_{id:D3}";

    private string GenerateNewLabel() =>
        $"_local_{_idGenerator.NewId:D3}";

    private void WriteHeader()
    {
        _asm.Com("Generated using the FALSE.NET compiler.")
            .Com("=======================================");
    }

    private void WriteConstants()
    {
        _asm.Com("Constants:");
        foreach (var (key, value) in Macros)
        {
            _asm.Str($"%define {key} {value}");
        }
    }

    private void WriteText(Program program)
    {
        _asm.Com("Code:")
            .Sec(ESection.Text);
        foreach (var label in _config.StartLabels)
            _asm.Str($"    global {label}");

        foreach (var id in program.Functions.Keys)
        {
            CompileLambda(program, id);
        }

        WriteDecimalConverter();
        WritePrintCharacter();
        WriteFlushStdout();
    }

    private void WriteSetup()
    {
        _asm.Com("===[SETUP START]===")
            .Com("Allocate FALSE stack:", true)
            .Mov(Rax, "SYS_MMAP")
            .Zro(Rdi)
            .Mov(Rsi, _config.StackSize)
            .Mov(Rdx, "PROT_READ | PROT_WRITE")
            .Mov(R10, "MAP_PRIVATE | MAP_ANONYMOUS")
            .Mov(R8, -1)
            .Zro(R9)
            .Syscall()
            .Com("Check for mmap success", true)
            .Cmp(Rax, -1)
            .Jne("skip_mmap_error")
            .Com(@"Print the fact that mmap failed", true);
        PrintString("mmap_error", "mmap_error_len", 2);
        Exit(1);
        _asm.Lbl(@"skip_mmap_error")
            .Mov(new LabelAddress("stack"), Rax)
            .Com(@"===[SETUP  END]===")
            .Str();
    }

    private void CompileLambda(Program program, long lambdaId)
    {
        _asm.Str();
        if (lambdaId == program.EntryId)
        {
            foreach (var label in _config.StartLabels)
                _asm.Lbl(label);
            WriteSetup();
        }
        else
        {
            _asm.Lbl(GetLabel(program, lambdaId));
        }

        var lambda = program.Functions[lambdaId];
        foreach (var command in lambda)
        {
            CompileCommand(program, command);
        }
    }

    private void CompileCommand(Program program, Command command)
    {
        var (operation, argument) = command;
        if (operation is Operation.WhileCondition or Operation.WhileBody)
            return;
        if (_config.WriteCommandComments)
            _asm.Com($"-- {command} --", true);

        switch (operation)
        {
            // Literals

            case Operation.IntLiteral:
                _asm.Mov(Rax, argument);
                Push("rax");
                break;

            // Stack

            case Operation.Dup:
                Peek("rax");
                Push("rax");
                break;

            case Operation.Drop:
                Drop();
                break;

            case Operation.Swap:
                _asm.Mov(Rbx, new LabelAddress("stack"))
                    .Mov(Rcx, new LabelAddress("stack_ptr"))
                    .Mov(Rax, new Address(Rbx, Rcx, -1, 8))
                    .Mov(Rdx, "[rbx+(rcx-2)*8]")
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rdx)
                    .Mov(new Address(Rbx, Rcx, -2, 8), Rax);
                break;

            case Operation.Rot:
                _asm.Mov(Rbx, new LabelAddress("stack"))
                    .Mov(Rcx, new LabelAddress("stack_ptr"))
                    .Mov(Rax, new Address(Rbx, Rcx, -1, 8))
                    .Mov(Rdx, new Address(Rbx, Rcx, -2, 8))
                    .Mov(Rsi, new Address(Rbx, Rcx, -3, 8))
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rsi)
                    .Mov(new Address(Rbx, Rcx, -2, 8), Rax)
                    .Mov(new Address(Rbx, Rcx, -3, 8), Rdx);
                break;

            case Operation.Pick:
                Peek("rax"); // Offset
                _asm.Mov(Rsi, Rcx)
                    .Sub(Rsi, Rax)
                    .Mov(Rax, new Address(Rbx, Rsi, -2, 8))
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            // Arithmetic

            case Operation.Add:
                Pop("rax");
                _asm.Mov(Rdx, new Address(Rbx, Rcx, -1, 8))
                    .Add(Rax, Rdx)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            case Operation.Sub:
                Pop("rax");
                _asm.Mov(Rdx, new Address(Rbx, Rcx, -1, 8))
                    .Sub(Rdx, Rax)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rdx);
                break;

            case Operation.Mul:
                Pop("rax");
                _asm.Mov(Rdx, new Address(Rbx, Rcx, -1, 8))
                    .Mul(Rax, Rdx)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            case Operation.Div:
                Pop("rdi");
                _asm.Mov(Rax, new Address(Rbx, Rcx, -1, 8))
                    .Zro(Rdx)
                    .Mov(Rsi, Rdx)
                    .Not(Rsi)
                    .Cmp(Rax, 0)
                    .Ins(Mnemonic.CMovL, Rdx, Rsi)
                    .Div(Rdi)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            case Operation.Neg:
                Peek("rax");
                _asm.Neg(Rax)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            case Operation.And:
                Pop("rax");
                _asm.Mov(Rdx, new Address(Rbx, Rcx, -1, 8))
                    .And(Rax, Rdx)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            case Operation.Or:
                Pop("rax");
                _asm.Mov(Rdx, new Address(Rbx, Rcx, -1, 8))
                    .Or(Rax, Rdx)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            case Operation.Not:
                Peek("rax");
                _asm.Not(Rax)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            // Comparison

            case Operation.Eq:
                Pop("rax");
                _asm.Str(@"    mov rdx, [rbx+(rcx-1)*8]")
                    .Str("    cmp rax, rdx")
                    .Str("    mov rax, 0")
                    .Str("    mov rdx, 0xffffffffffffffff")
                    .Str("    cmove rax, rdx")
                    .Str("    mov [rbx+(rcx-1)*8], rax");
                break;

            case Operation.Gt:
                Pop("rax");
                _asm.Str(@"    mov rdx, [rbx+(rcx-1)*8]")
                    .Str("    cmp rax, rdx")
                    .Str("    mov rax, 0")
                    .Str("    mov rdx, 0xffffffffffffffff")
                    .Str("    cmovl rax, rdx")
                    .Str("    mov [rbx+(rcx-1)*8], rax");
                break;

            // Control Flow and Lambdas

            case Operation.Lambda:
                _asm.Str($"    lea rax, [rel {GetLabel(program, argument)}]");
                Push("rax");
                break;

            case Operation.Ret:
                _asm.Str("    ret");
                break;

            case Operation.Execute:
                Pop("rax");
                _asm.Str($"    call rax");
                break;

            case Operation.ConditionalExecute:
                var label = GenerateNewLabel();
                Pop("rax"); // body
                Pop("rdx"); // condition
                _asm.Str(@"    cmp rdx, 0")
                    .Str($"    jz {label}")
                    .Str(@"    call rax")
                    .Str($"{label}:");
                break;

            case Operation.WhileInit:
                Pop("rax"); // body
                _asm.Str("    push rax");
                Pop("rax"); // condition
                _asm.Str("    push rax");
                var loop = GenerateNewLabel();
                var condition = GenerateNewLabel();
                _asm.Str($"    jmp {condition}")
                    .Str($"{loop}:")
                    .Str(@"    mov rax, [rsp+8]")
                    .Str(@"    call rax")
                    .Str($"{condition}:")
                    .Str(@"    mov rax, [rsp]")
                    .Str(@"    call rax");
                Pop("rax");
                _asm.Str(@"    cmp rax, 0")
                    .Str($"    jnz {loop}")
                    .Str(@"    add rsp, 16");
                break;

            // Names

            case Operation.Ref:
                _asm.Str($"    mov rax, {argument}");
                Push("rax");
                break;

            case Operation.Store:
                Pop("rax");
                Pop("rdx");
                _asm.Str("    lea rbx, [rel references]")
                    .Str("    mov [rbx+rax*8], rdx");
                break;

            case Operation.Load:
                Peek("rax");
                _asm.Str("    lea rbx, [rel references]")
                    .Str("    mov rdx, [rbx+rax*8]");
                Replace("rdx");
                break;

            // I/O

            case Operation.PrintString:
                PrintString(argument);
                break;

            case Operation.OutputChar:
                Pop("rdi");
                _asm.Str("    call print_character");
                break;

            case Operation.OutputDecimal:
                Pop("rdi");
                _asm.Str("    call print_decimal");
                break;

            case Operation.Flush:
                _asm.Str("    call flush_stdout");
                break;

            case Operation.Exit:
                Exit(0); // maybe exit with top of FALSE stack?
                break;

            case Operation.WhileCondition or Operation.WhileBody:
                throw new CompilerException("Unreachable");
        }
    }

    private void WriteDecimalConverter()
    {
        _asm.Str("")
            .Str("; Converts rdi to decimal and writes to stdout.")
            .Str("print_decimal:")

            // For now, flush stdout. In the future, this function will also write into the buffer.
            .Str("    push rdi")
            .Str("    call flush_stdout")
            .Str("    pop rdi")

            // rax: number, rsi: isNegative, rbx: string base, rcx: string index
            // rdx: modulo

            // 1. take the absolute and remember if number was negative.
            .Str("    mov rax, rdi")
            .Str("    neg rdi")
            .Str("    mov r11, 0")
            .Str("    mov rdx, -1")
            .Str("    cmp rax, 0")
            .Str("    cmovl r11, rdx")
            .Str("    cmovl rax, rdi")

            // 2. Convert to decimal and store in string_buffer. (right to left)
            //    Count decimal places.
            .Str("    mov rdi, 10")
            .Str("    lea r8, [rel string_buffer]")
            .Str($"    mov rcx, 0x{_config.StringBufferSize:x8}") // Start at the end
            .Str("print_decimal_loop:")
            .Str("    dec rcx")
            .Str("    xor rdx, rdx")
            .Str("    div rdi") // digit in rdi
            .Str("    add dl, '0'")
            .Str("    mov [r8,rcx], dl")
            .Str("    cmp rax, 0")
            .Str("    jne print_decimal_loop")

            // 3. Write '-' in front if number was negative.
            //    also, increment length counter
            .Str("    cmp r11, 0")
            .Str("    je print_decimal_skip")
            .Str("    dec rcx")
            .Str("    mov dl, '-'")
            .Str("    mov [r8,rcx], dl")
            .Str("print_decimal_skip:")

            // 4. Pass string_buffer+32-length as pointer, length to write syscall.
            .Str("    add r8, rcx")
            .Str($"    mov rdx, 0x{_config.StringBufferSize:x8}")
            .Str("    sub rdx, rcx")
            .Str("    mov rax, SYS_WRITE")
            .Str("    mov rdi, 1")
            .Str("    mov rsi, r8")
            .Str("    syscall")
            .Str("    ret");
    }

    private void WritePrintCharacter()
    {
        _asm.Str("")
            .Str("; Prints character located in dil.")
            .Str("print_character:")
            .Str("    lea rsi, [rel stdout_buffer]")
            .Str("    xor rdx, rdx")
            .Str("    mov dx, [rel stdout_len]")
            .Str("    mov [rsi+rdx], rdi")
            .Str("    inc dx")
            .Str("    mov r8b, 0")
            .Str("    mov r9b, 0")
            .Str("    mov r10b, 0xff")
            .Str($"    cmp dx, {_config.StdoutBufferSize}")
            .Str("    cmove r8, r10")
            .Str("    cmp dil, 10") // flush on newlines
            .Str("    cmove r9, r10")
            .Str("    or r8b, r9b")
            .Str("    cmp r8b, 0")
            .Str("    jz print_character_ret")
            .Str("    mov rax, SYS_WRITE")
            .Str("    mov rdi, 1")
            // rsi is already pointing to the string
            // rdx is already the length of the string
            .Str("    syscall")
            .Str("    mov dx, 0")
            .Str("print_character_ret:")
            .Str("    mov [rel stdout_len], dx")
            .Str("    ret");
    }

    private void WriteFlushStdout()
    {
        _asm.Str("")
            .Str("; Flushes stdout.")
            .Str("flush_stdout:")
            .Str("    lea rsi, [rel stdout_buffer]")
            .Str("    xor rdx, rdx")
            .Str("    mov dx, [rel stdout_len]")
            .Str("    cmp dx, 0")
            .Str("    jz flush_stdout_ret")
            .Str("    mov rax, SYS_WRITE")
            .Str("    mov rdi, 1")
            .Str("    syscall")
            .Str("    mov dx, 0")
            .Str("    mov [rel stdout_len], dx")
            .Str("flush_stdout_ret:")
            .Str("    ret");
    }

    /*****************************************\
     *            Common Patterns            *
     *****************************************/

    private void PrintString(long id, int fd = 1)
    {
        PrintString(GetStringLabel(id), GetStringLenLabel(id), fd);
    }

    private void PrintString(string strLabel, string lenLabel, int fd = 1)
    {
        _asm.Str(@"    push rdi")
            .Str(@"    call flush_stdout")
            .Str(@"    pop rdi")
            .Str(@"    mov rax, SYS_WRITE")
            .Str($"    mov rdi, {fd}") // stdo
            .Str($"    lea rsi, [rel {strLabel}]")
            .Str($"    mov rdx, {lenLabel}")
            .Str(@"    syscall");
    }

    private void Exit(int exitCode = 0)
    {
        Exit($"{exitCode}");
    }

    private void Exit(string exitCode)
    {
        _asm.Str(@"    mov rax, SYS_EXIT")
            .Str($"    mov rdi, {exitCode}")
            .Str(@"    syscall");
    }

    private void Push(string register)
    {
        _asm.Str(@"    mov rbx, [rel stack]")
            .Str(@"    mov rcx, [rel stack_ptr]")
            .Str($"    mov [rbx,rcx*8], {register}")
            .Str(@"    inc rcx")
            .Str(@"    mov [rel stack_ptr], rcx");
    }

    private void Pop(string register)
    {
        _asm.Str(@"    mov rbx, [rel stack]")
            .Str(@"    mov rcx, [rel stack_ptr]")
            .Str(@"    dec rcx")
            .Str($"    mov {register}, [rbx,rcx*8]")
            .Str(@"    mov [rel stack_ptr], rcx");
    }

    private void Drop()
    {
        _asm.Str(@"    mov rcx, [rel stack_ptr]")
            .Str(@"    dec rcx")
            .Str(@"    mov [rel stack_ptr], rcx");
    }

    private void Replace(string register)
    {
        _asm.Str(@"    mov rbx, [rel stack]")
            .Str(@"    mov rcx, [rel stack_ptr]")
            .Str($"    mov [rbx+(rcx-1)*8], {register}");
    }

    private void Peek(string register)
    {
        _asm.Str(@"    mov rbx, [rel stack]")
            .Str(@"    mov rcx, [rel stack_ptr]")
            .Str($"    mov {register}, [rbx+(rcx-1)*8]");
    }

    /*****************************************\
     *             Data Sections             *
     *****************************************/

    private void WriteBss()
    {
        _asm.Str("; Uninitialized Globals:")
            .Str("    section .bss")
            .Str("references: RESQ 32")
            .Str("stack: RESQ 1")
            .Str("string_buffer: RESB 32")
            .Str($"stdout_buffer: RESB {_config.StdoutBufferSize}");
    }

    private void WriteData()
    {
        _asm.Str("; Globals:")
            .Str("    section .data")
            .Str("stack_ptr: DQ 0")
            .Str("stdout_len: DW 0");
    }

    private void WriteRoData(Program program)
    {
        _asm.Str("; Constants:")
            .Str("    section .rodata");
        foreach (var entry in program.Strings)
        {
            var (key, value) = entry;
            _asm.Str($"{GetStringLabel(key)}: DB {value.Escape('`')}")
                .Str($"{GetStringLenLabel(key)}: EQU $ - {GetStringLabel(key)}");
        }

        _asm.Str();
        _asm.Com("Constant Constants:");
        foreach (var entry in Strings)
        {
            var (key, value) = entry;
            _asm.Str($"{key}: DB {value.Escape('`')}")
                .Str($"{key}_len: EQU $ - {key}");
        }
    }
}