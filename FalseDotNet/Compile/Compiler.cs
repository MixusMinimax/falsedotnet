using System.Diagnostics.CodeAnalysis;
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
    private static readonly Register Dx = new Register(ERegister.dx, ERegisterSize.w);
    private static readonly Register Dl = new Register(ERegister.dx, ERegisterSize.l);
    private static readonly Register Rdi = new Register(ERegister.di, ERegisterSize.r);
    private static readonly Register Dil = new Register(ERegister.di, ERegisterSize.l);
    private static readonly Register Rsi = new Register(ERegister.si, ERegisterSize.r);
    private static readonly Register R8 = new Register(ERegister.r8, ERegisterSize.r);
    private static readonly Register R8B = new Register(ERegister.r8, ERegisterSize.l);
    private static readonly Register R9 = new Register(ERegister.r9, ERegisterSize.r);
    private static readonly Register R9B = new Register(ERegister.r9, ERegisterSize.l);
    private static readonly Register R10 = new Register(ERegister.r10, ERegisterSize.r);
    private static readonly Register R11 = new Register(ERegister.r11, ERegisterSize.r);
    private static readonly Register Rsp = new Register(ERegister.sp, ERegisterSize.r);

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
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

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
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

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
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
                Push(Rax);
                break;

            // Stack

            case Operation.Dup:
                Peek(Rax);
                Push(Rax);
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
                Peek(Rax); // Offset
                _asm.Mov(Rsi, Rcx)
                    .Sub(Rsi, Rax)
                    .Mov(Rax, new Address(Rbx, Rsi, -2, 8))
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            // Arithmetic

            case Operation.Add:
                Pop(Rax);
                _asm.Mov(Rdx, new Address(Rbx, Rcx, -1, 8))
                    .Add(Rax, Rdx)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            case Operation.Sub:
                Pop(Rax);
                _asm.Mov(Rdx, new Address(Rbx, Rcx, -1, 8))
                    .Sub(Rdx, Rax)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rdx);
                break;

            case Operation.Mul:
                Pop(Rax);
                _asm.Mov(Rdx, new Address(Rbx, Rcx, -1, 8))
                    .Mul(Rax, Rdx)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            case Operation.Div:
                Pop(Rdi);
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
                Peek(Rax);
                _asm.Neg(Rax)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            case Operation.And:
                Pop(Rax);
                _asm.Mov(Rdx, new Address(Rbx, Rcx, -1, 8))
                    .And(Rax, Rdx)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            case Operation.Or:
                Pop(Rax);
                _asm.Mov(Rdx, new Address(Rbx, Rcx, -1, 8))
                    .Or(Rax, Rdx)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            case Operation.Not:
                Peek(Rax);
                _asm.Not(Rax)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            // Comparison

            case Operation.Eq:
                Pop(Rax);
                _asm.Mov(Rdx, new Address(Rbx, Rcx, -1, 8))
                    .Cmp(Rax, Rdx)
                    .Mov(Rax, 0) // "xor rax, rax" would affect the status register
                    .Mov(Rdx, -1)
                    .Ins(Mnemonic.CMovE, Rax, Rdx)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            case Operation.Gt:
                Pop(Rax);
                _asm.Mov(Rdx, new Address(Rbx, Rcx, -1, 8))
                    .Cmp(Rax, Rdx)
                    .Mov(Rax, 0)
                    .Mov(Rdx, -1)
                    .Ins(Mnemonic.CMovL, Rax, Rdx)
                    .Mov(new Address(Rbx, Rcx, -1, 8), Rax);
                break;

            // Control Flow and Lambdas

            case Operation.Lambda:
                _asm.Lea(Rax, GetLabel(program, argument));
                Push(Rax);
                break;

            case Operation.Ret:
                _asm.Ins(Mnemonic.Ret);
                break;

            case Operation.Execute:
                Pop(Rax);
                _asm.Ins(Mnemonic.Call, Rax);
                break;

            case Operation.ConditionalExecute:
                var label = GenerateNewLabel();
                Pop(Rax); // body
                Pop(Rdx); // condition
                _asm.Cmp(Rdx, 0)
                    .Jz(label)
                    .Ins(Mnemonic.Call, Rax)
                    .Lbl(label);
                break;

            case Operation.WhileInit:
                Pop(Rax); // body
                _asm.Ins(Mnemonic.Push, Rax);
                Pop(Rax); // condition
                _asm.Ins(Mnemonic.Push, Rax);
                var loop = GenerateNewLabel();
                var condition = GenerateNewLabel();
                _asm.Jmp(condition)
                    .Lbl(loop)
                    .Mov(Rax, new Address(Rsp, AddressOffset: 8))
                    .Ins(Mnemonic.Call, Rax)
                    .Lbl(condition)
                    .Mov(Rax, new Address(Rsp))
                    .Ins(Mnemonic.Call, Rax);
                Pop(Rax);
                _asm.Cmp(Rax, 0)
                    .Jne(loop)
                    .Add(Rsp, 16);
                break;

            // Names

            case Operation.Ref:
                _asm.Mov(Rax, argument);
                Push(Rax);
                break;

            case Operation.Store:
                Pop(Rax);
                Pop(Rdx);
                _asm.Lea(Rbx, "references")
                    .Mov(new Address(Rbx, Rax, Stride: 8), Rdx);
                break;

            case Operation.Load:
                Peek(Rax);
                _asm.Lea(Rbx, "references")
                    .Mov(Rdx, new Address(Rbx, Rax, Stride: 8));
                Replace(Rdx);
                break;

            // I/O

            case Operation.PrintString:
                PrintString(argument);
                break;

            case Operation.OutputChar:
                Pop(Rdi);
                _asm.Cll("print_character");
                break;

            case Operation.OutputDecimal:
                Pop(Rdi);
                _asm.Cll("print_decimal");
                break;

            case Operation.Flush:
                _asm.Cll("flush_stdout");
                break;

            case Operation.Exit:
                Exit(); // maybe exit with top of FALSE stack?
                break;

            case Operation.WhileCondition or Operation.WhileBody:
                throw new CompilerException("Unreachable");
        }
    }

    private void WriteDecimalConverter()
    {
        _asm.Str()
            .Com("Converts rdi to decimal and writes to stdout.")
            .Lbl("print_decimal")

            // For now, flush stdout. In the future, this function will also write into the buffer.
            .Ins(Mnemonic.Push, Rdi)
            .Cll("flush_stdout")
            .Ins(Mnemonic.Pop, Rdi)

            // rax: number, rsi: isNegative, rbx: string base, rcx: string index
            // rdx: modulo

            // 1. take the absolute and remember if number was negative.
            .Mov(Rax, Rdi)
            .Neg(Rdi)
            .Zro(R11)
            .Mov(Rdx, -1)
            .Cmp(Rax, 0)
            .Ins(Mnemonic.CMovL, R11, Rdx)
            .Ins(Mnemonic.CMovL, Rax, Rdi)

            // 2. Convert to decimal and store in string_buffer. (right to left)
            //    Count decimal places.
            .Mov(Rdi, 10)
            .Lea(R8, "string_buffer")
            .Mov(Rcx, _config.StringBufferSize) // Start at the end
            .Lbl("print_decimal_loop")
            .Dec(Rcx)
            .Zro(Rdx)
            .Div(Rdi) // digit in rdi
            .Add(Dl, '0')
            .Mov(new Address(R8, Rcx), Dl)
            .Cmp(Rax, 0)
            .Jne("print_decimal_loop")

            // 3. Write '-' in front if number was negative.
            //    also, increment length counter
            .Cmp(R11, 0)
            .Je("print_decimal_skip")
            .Dec(Rcx)
            .Mov(Dl, '-')
            .Mov(new Address(R8, Rcx), Dl)
            .Lbl("print_decimal_skip")

            // 4. Pass string_buffer+32-length as pointer, length to write syscall.
            .Add(R8, Rcx)
            .Mov(Rdx, _config.StringBufferSize)
            .Sub(Rdx, Rcx)
            .Mov(Rax, "SYS_WRITE")
            .Mov(Rdi, 1)
            .Mov(Rsi, R8)
            .Syscall()
            .Ins(Mnemonic.Ret);
    }

    private void WritePrintCharacter()
    {
        _asm.Str()
            .Com("Prints character located in dil.")
            .Lbl("print_character")
            .Lea(Rsi, "stdout_buffer")
            .Zro(Rdx)
            .Mov(Dx, new LabelAddress("stdout_len"))
            .Mov(new Address(Rsi, Rdx), Rdi)
            .Inc(Dx)
            .Mov(new Register(ERegister.r8, ERegisterSize.l), 0)
            .Mov(new Register(ERegister.r9, ERegisterSize.l), 0)
            .Mov(new Register(ERegister.r9, ERegisterSize.l), -1)
            .Cmp(Dx, _config.StdoutBufferSize)
            .Ins(Mnemonic.CMovE, R8, R10)
            .Cmp(Dil, 10) // flush on newlines
            .Ins(Mnemonic.CMovE, R9, R10)
            .Or(R8B, R9B)
            .Cmp(R8B, 0)
            .Jz("print_character_ret")
            .Mov(Rax, "SYS_WRITE")
            .Mov(Rdi, 1)
            // rsi is already pointing to the string
            // rdx is already the length of the string
            .Syscall()
            .Zro(Dx)
            .Lbl("print_character_ret")
            .Mov(new LabelAddress("stdout_len"), Dx)
            .Ins(Mnemonic.Ret);
    }

    private void WriteFlushStdout()
    {
        _asm.Str()
            .Com("Flushes stdout.")
            .Lbl("flush_stdout")
            .Lea(Rsi, "stdout_buffer")
            .Zro(Rdx)
            .Mov(Dx, new LabelAddress("stdout_len"))
            .Cmp(Dx, 0)
            .Jz("flush_stdout_ret")
            .Mov(Rax, "SYS_WRITE")
            .Mov(Rdi, 1)
            .Syscall()
            .Zro(Dx)
            .Mov(new LabelAddress("stdout_len"), Dx)
            .Lbl("flush_stdout_ret")
            .Ins(Mnemonic.Ret);
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
        _asm.Ins(Mnemonic.Push, Rdi)
            .Cll("flush_stdout")
            .Ins(Mnemonic.Pop, Rdi)
            .Mov(Rax, "SYS_WRITE")
            .Mov(Rdi, fd) // stdout
            .Lea(Rsi, strLabel)
            .Mov(Rdx, new LabelOperand(lenLabel))
            .Syscall();
    }

    private void Exit(long exitCode = 0)
    {
        Exit(new Literal(exitCode));
    }

    private void Exit(IOperand exitCode)
    {
        _asm.Mov(Rax, "SYS_EXIT")
            .Mov(Rdi, exitCode)
            .Syscall();
    }

    private void Push(IOperand register)
    {
        _asm.Mov(Rbx, new LabelAddress("stack"))
            .Mov(Rcx, new LabelAddress("stack_ptr"))
            .Mov(new Address(Rbx, Rcx, Stride: 8), register)
            .Inc(Rcx)
            .Mov(new LabelAddress("stack_ptr"), Rcx);
    }

    private void Pop(IOperand register)
    {
        _asm.Mov(Rbx, new LabelAddress("stack"))
            .Mov(Rcx, new LabelAddress("stack_ptr"))
            .Dec(Rcx)
            .Mov(register, new Address(Rbx, Rcx, Stride: 8))
            .Mov(new LabelAddress("stack_ptr"), Rcx);
    }

    private void Drop()
    {
        _asm.Mov(Rcx, new LabelAddress("stack_ptr"))
            .Dec(Rcx)
            .Mov(new LabelAddress("stack_ptr"), Rcx);
    }

    private void Replace(IOperand register)
    {
        _asm.Mov(Rbx, new LabelAddress("stack"))
            .Mov(Rcx, new LabelAddress("stack_ptr"))
            .Mov(new Address(Rbx, Rcx, -1, 8), register);
    }

    private void Peek(IOperand register)
    {
        _asm.Mov(Rbx, new LabelAddress("stack"))
            .Mov(Rcx, new LabelAddress("stack_ptr"))
            .Mov(register, new Address(Rbx, Rcx, -1, 8));
    }

    /*****************************************\
     *             Data Sections             *
     *****************************************/

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private void WriteBss()
    {
        // I can leave these lines as literal strings, as they won't be optimized.
        _asm.Com("Uninitialized Globals:")
            .Sec(ESection.Bss)
            .Str("references: RESQ 32")
            .Str("stack: RESQ 1")
            .Str("string_buffer: RESB 32")
            .Str($"stdout_buffer: RESB {_config.StdoutBufferSize}");
    }

    private void WriteData()
    {
        _asm.Com("Globals:")
            .Sec(ESection.Data)
            .Str("stack_ptr: DQ 0")
            .Str("stdout_len: DW 0");
    }

    private void WriteRoData(Program program)
    {
        _asm.Com("Constants:")
            .Sec(ESection.RoData);
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