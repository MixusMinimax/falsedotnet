using System.Diagnostics.CodeAnalysis;
using FalseDotNet.Commands;
using FalseDotNet.Compile.Instructions;
using FalseDotNet.Compile.Optimization;
using FalseDotNet.Parse;
using FalseDotNet.Utility;

namespace FalseDotNet.Compile;

public class Compiler : ICompiler
{
    // @formatter:int_align_fields true
    private static readonly Register Rax           = new(ERegister.ax, ERegisterSize.r);
    private static readonly Register Al            = new(ERegister.ax, ERegisterSize.l);
    private static readonly Register Rbx           = new(ERegister.bx, ERegisterSize.r);
    private static readonly Register Rcx           = new(ERegister.cx, ERegisterSize.r);
    private static readonly Register Rdx           = new(ERegister.dx, ERegisterSize.r);
    private static readonly Register Dx            = new(ERegister.dx, ERegisterSize.w);
    private static readonly Register Dl            = new(ERegister.dx, ERegisterSize.l);
    private static readonly Register Rdi           = new(ERegister.di, ERegisterSize.r);
    private static readonly Register Dil           = new(ERegister.di, ERegisterSize.l);
    private static readonly Register Rsi           = new(ERegister.si, ERegisterSize.r);
    private static readonly Register Sil           = new(ERegister.si, ERegisterSize.l);
    private static readonly Register R8            = new(ERegister.r8, ERegisterSize.r);
    private static readonly Register R8B           = new(ERegister.r8, ERegisterSize.l);
    private static readonly Register R9            = new(ERegister.r9, ERegisterSize.r);
    private static readonly Register R9B           = new(ERegister.r9, ERegisterSize.l);
    private static readonly Register R10           = new(ERegister.r10, ERegisterSize.r);
    private static readonly Register StackCounter  = new(ERegister.r12, ERegisterSize.r);
    private static readonly Register StackBase     = new(ERegister.r13, ERegisterSize.r);
    private static readonly Register TypeStackBase = new(ERegister.r14, ERegisterSize.r);
    private static readonly Register CurType       = new(ERegister.r15, ERegisterSize.l);

    private static readonly Register Rsp = new(ERegister.sp, ERegisterSize.r);
    // @formatter:int_align_fields restore

    // @formatter:int_align_assignments true
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private static readonly Dictionary<string, string> Macros = new()
    {
        ["SYS_READ"]      = "0",
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
        ["mmap_error"]            = "MMAP Failed! Exiting.\n",
        ["err_msg_pop_number"]    = "Tried to pop Number from stack!\n",
        ["err_msg_pop_lambda"]    = "Tried to pop Lambda from stack!\n",
        ["err_msg_pop_reference"] = "Tried to pop Reference from stack!\n",
    };
    // @formatter:int_align_assignments restore

    private readonly ILogger _logger;
    private readonly IIdGenerator _idGenerator;
    private readonly IOptimizer _optimizer;
    private CompilerConfig _config = null!;
    private Asm _asm = null!;
    private Program _program = null!;

    public Compiler(ILogger logger, IIdGenerator idGenerator, IOptimizer optimizer)
    {
        _logger = logger;
        _idGenerator = idGenerator;
        _optimizer = optimizer;
    }

    public void Compile(Program program, TextWriter output, CompilerConfig config)
    {
        _logger.WriteLine($"  Type Safety: {config.TypeSafety}");

        _config = config;
        _asm = new Asm();
        _program = program;

        WriteHeader();
        _asm.Str();
        WriteConstants();
        _asm.Str();
        WriteText();
        _asm.Str();
        WriteBss();
        _asm.Str();
        WriteData();
        _asm.Str();
        WriteRoData();

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

    private void WriteText()
    {
        _asm.Com("Code:")
            .Sec(ESection.Text);
        foreach (var label in _config.StartLabels)
            _asm.Str($"    global {label}");

        foreach (var id in _program.Functions.Keys)
        {
            CompileLambda(_program, id);
        }

        WriteDecimalConverter();
        WritePrintCharacter();
        WritePrintString();
        WriteFlushStdout();
    }

    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    private void WriteSetup()
    {
        void MMap(long size)
        {
            var skipLabel = $"skip_mmap_error_{_idGenerator.NewId}";
            _asm.Mov(Rax, "SYS_MMAP")
                .Zro(Rdi)
                .Mov(Rsi, size)
                .Mov(Rdx, "PROT_READ | PROT_WRITE")
                .Mov(R10, "MAP_PRIVATE | MAP_ANONYMOUS")
                .Mov(R8, -1)
                .Zro(R9)
                .Syscall()
                .Com("Check for mmap success", true)
                .Cmp(Rax, 0)
                .Jge(skipLabel)
                .Com("Print the fact that mmap failed", true);
            PrintString("mmap_error", "mmap_error_len", 2);
            Exit(1);
            _asm.Lbl(skipLabel);
        }

        _asm.Com("===[SETUP START]===")
            .Com("Allocate FALSE stack:", true);
        MMap(_config.StackSize);
        _asm.Mov(StackBase, Rax);

        if (_config.TypeSafety is not TypeSafety.None)
        {
            _asm.Str()
                .Com("Allocate Type stack:", true);
            MMap(_config.StackSize / 8);
            _asm.Mov(TypeStackBase, Rax);
        }

        _asm.Com(@"===[SETUP  END]===")
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
                Push(Rax, StackElementType.Number);
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
                _asm.Mov(Rax, new Address(StackBase, StackCounter, -1, 8))
                    .Mov(Rdx, new Address(StackBase, StackCounter, -2, 8))
                    .Mov(new Address(StackBase, StackCounter, -1, 8), Rdx)
                    .Mov(new Address(StackBase, StackCounter, -2, 8), Rax);
                if (_config.TypeSafety is not TypeSafety.None)
                {
                    _asm.Mov(Al, new Address(TypeStackBase, StackCounter, -1))
                        .Mov(Dl, new Address(TypeStackBase, StackCounter, -2))
                        .Mov(new Address(TypeStackBase, StackCounter, -1), Dl)
                        .Mov(new Address(TypeStackBase, StackCounter, -2), Al);
                }

                break;

            case Operation.Rot:
                _asm.Mov(Rax, new Address(StackBase, StackCounter, -1, 8))
                    .Mov(Rdx, new Address(StackBase, StackCounter, -2, 8))
                    .Mov(Rsi, new Address(StackBase, StackCounter, -3, 8))
                    .Mov(new Address(StackBase, StackCounter, -1, 8), Rsi)
                    .Mov(new Address(StackBase, StackCounter, -2, 8), Rax)
                    .Mov(new Address(StackBase, StackCounter, -3, 8), Rdx);
                if (_config.TypeSafety is not TypeSafety.None)
                {
                    _asm.Mov(Al, new Address(TypeStackBase, StackCounter, -1))
                        .Mov(Dl, new Address(TypeStackBase, StackCounter, -2))
                        .Mov(Sil, new Address(TypeStackBase, StackCounter, -3))
                        .Mov(new Address(TypeStackBase, StackCounter, -1), Sil)
                        .Mov(new Address(TypeStackBase, StackCounter, -2), Al)
                        .Mov(new Address(TypeStackBase, StackCounter, -3), Dl);
                }

                break;

            case Operation.Pick:
                Peek(Rax, StackElementType.Number); // Offset
                _asm.Mov(Rsi, StackCounter)
                    .Sub(Rsi, Rax)
                    .Mov(Rax, new Address(StackBase, Rsi, -2, 8));
                if (_config.TypeSafety is not TypeSafety.None)
                {
                    _asm.Mov(CurType, new Address(TypeStackBase, Rsi, -2));
                }

                Replace(Rax);
                break;

            // Arithmetic

            case Operation.Add:
                Pop(Rax, StackElementType.Number);
                Peek(Rdx, StackElementType.Number);
                _asm.Add(Rax, Rdx);
                Replace(Rax, StackElementType.Number);
                break;

            case Operation.Sub:
                Pop(Rax, StackElementType.Number);
                Peek(Rdx, StackElementType.Number);
                _asm.Sub(Rdx, Rax);
                Replace(Rdx, StackElementType.Number);
                break;

            case Operation.Mul:
                Pop(Rax, StackElementType.Number);
                Peek(Rdx, StackElementType.Number);
                _asm.Mul(Rax, Rdx);
                Replace(Rax, StackElementType.Number);
                break;

            case Operation.Div:
                Pop(Rdi, StackElementType.Number);
                Peek(Rax, StackElementType.Number);
                _asm.Zro(Rdx)
                    .Mov(Rsi, Rdx)
                    .Not(Rsi)
                    .Cmp(Rax, 0)
                    .Ins(Mnemonic.CMovL, Rdx, Rsi)
                    .Div(Rdi);
                Replace(Rax, StackElementType.Number);
                break;

            case Operation.Neg:
                Peek(Rax, StackElementType.Number);
                _asm.Neg(Rax);
                Replace(Rax, StackElementType.Number);
                break;

            case Operation.And:
                Pop(Rax, StackElementType.Number);
                Peek(Rdx, StackElementType.Number);
                _asm.And(Rax, Rdx);
                Replace(Rax, StackElementType.Number);
                break;

            case Operation.Or:
                Pop(Rax, StackElementType.Number);
                Peek(Rdx, StackElementType.Number);
                _asm.Or(Rax, Rdx);
                Replace(Rax, StackElementType.Number);
                break;

            case Operation.Not:
                Peek(Rax, StackElementType.Number);
                _asm.Not(Rax);
                Replace(Rax, StackElementType.Number);
                break;

            // Comparison

            case Operation.Eq:
                Pop(Rax, StackElementType.Number);
                Peek(Rdx, StackElementType.Number);
                _asm.Cmp(Rax, Rdx)
                    .Mov(Rax, 0) // "xor rax, rax" would affect the status register
                    .Mov(Rdx, -1)
                    .Ins(Mnemonic.CMovE, Rax, Rdx);
                Replace(Rax);
                break;

            case Operation.Gt:
                Pop(Rax, StackElementType.Number);
                Peek(Rdx, StackElementType.Number);
                _asm.Cmp(Rax, Rdx)
                    .Mov(Rax, 0) // "xor rax, rax" would affect the status register
                    .Mov(Rdx, -1)
                    .Ins(Mnemonic.CMovL, Rax, Rdx);
                Replace(Rax);
                break;

            // Control Flow and Lambdas

            case Operation.Lambda:
                _asm.Lea(Rax, GetLabel(program, argument));
                Push(Rax, StackElementType.Lambda);
                break;

            case Operation.Ret:
                _asm.Ins(Mnemonic.Ret);
                break;

            case Operation.Execute:
                Pop(Rax, StackElementType.Lambda);
                _asm.Ins(Mnemonic.Call, Rax);
                break;

            case Operation.ConditionalExecute:
                var label = GenerateNewLabel();
                Pop(Rax, StackElementType.Lambda); // body
                Pop(Rdx, StackElementType.Number); // condition
                _asm.Cmp(Rdx, 0)
                    .Jz(label)
                    .Ins(Mnemonic.Call, Rax)
                    .Lbl(label);
                break;

            case Operation.WhileInit:
                Pop(Rax, StackElementType.Lambda); // body
                _asm.Ins(Mnemonic.Push, Rax);
                Pop(Rax, StackElementType.Lambda); // condition
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
                Pop(Rax, StackElementType.Number);
                _asm.Cmp(Rax, 0)
                    .Jne(loop)
                    .Add(Rsp, 16);
                break;

            // Names

            case Operation.Ref:
                _asm.Mov(Rax, argument);
                Push(Rax, StackElementType.Reference);
                break;

            case Operation.Store:
                Pop(Rax, StackElementType.Reference);
                Pop(Rdx);
                _asm.And(Rax, 0b11111)
                    .Lea(Rbx, "references")
                    .Mov(new Address(Rbx, Rax, Stride: 8), Rdx);
                if (_config.TypeSafety is not TypeSafety.None)
                {
                    _asm.Lea(Rbx, "type_references")
                        .Mov(new Address(Rbx, Rax), CurType);
                }

                break;

            case Operation.Load:
                Peek(Rax, StackElementType.Reference);
                _asm.And(Rax, 0b11111)
                    .Lea(Rbx, "references")
                    .Mov(Rdx, new Address(Rbx, Rax, Stride: 8));
                if (_config.TypeSafety is not TypeSafety.None)
                {
                    _asm.Lea(Rbx, "type_references")
                        .Mov(CurType, new Address(Rbx, Rax));
                }

                Replace(Rdx);
                break;

            // I/O

            case Operation.ReadChar:
                _asm.Mov(Rax, "SYS_READ")
                    .Mov(Rdi, 0)
                    .Zro(Rsi)
                    .Mov(new Address(StackBase, StackCounter, Stride: 8), Rsi)
                    .Lea(Rsi, new Address(StackBase, StackCounter, Stride: 8))
                    .Mov(Rdx, 1)
                    .Syscall();
                if (_config.TypeSafety is not TypeSafety.None)
                {
                    _asm.Mov(CurType, (long)StackElementType.Number)
                        .Mov(new Address(TypeStackBase, StackCounter), CurType);
                }

                var lbl = GenerateNewLabel();
                _asm.Cmp(Rax, 0)
                    .Jne(lbl)
                    .Mov(Rax, -1)
                    .Mov(new Address(StackBase, StackCounter, Stride: 8), Rax)
                    .Lbl(lbl)
                    .Inc(StackCounter);
                break;

            case Operation.PrintString:
                PrintStringById(argument);
                break;

            case Operation.OutputChar:
                Pop(Rdi, StackElementType.Number);
                _asm.Cll("print_character");
                break;

            case Operation.OutputDecimal:
                Pop(Rdi, StackElementType.Number);
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

            // rax: number, rsi: isNegative, rbx: string base, rcx: string index
            // rdx: modulo

            // 1. take the absolute and remember if number was negative.
            .Mov(Rax, Rdi)
            .Neg(Rdi)
            .Zro(R10)
            .Mov(Rdx, -1)
            .Cmp(Rax, 0)
            .Ins(Mnemonic.CMovL, R10, Rdx)
            .Ins(Mnemonic.CMovL, Rax, Rdi)

            // 2. Convert to decimal and store in string_buffer. (right to left)
            //    Count decimal places.
            .Mov(Rdi, 10)
            .Lea(Rsi, "string_buffer")
            .Mov(Rcx, _config.StringBufferSize) // Start at the end
            .Lbl("print_decimal_loop")
            .Dec(Rcx)
            .Zro(Rdx)
            .Div(Rdi) // digit in rdi
            .Add(Dl, '0')
            .Mov(new Address(Rsi, Rcx), Dl)
            .Cmp(Rax, 0)
            .Jne("print_decimal_loop")

            // 3. Write '-' in front if number was negative.
            //    also, increment length counter
            .Cmp(R10, 0)
            .Je("print_decimal_skip")
            .Dec(Rcx)
            .Mov(R9B, '-')
            .Mov(new Address(Rsi, Rcx), R9B)
            .Lbl("print_decimal_skip")

            // 4. Pass string_buffer+32-length as pointer, length to write syscall.
            .Add(Rsi, Rcx)
            .Mov(Rdx, _config.StringBufferSize)
            .Sub(Rdx, Rcx)
            .Cmp(Rdx, _config.StdoutBufferSize)
            .Jl("print_decimal_skip_syscall")
            .Ins(Mnemonic.Push, Rsi)
            .Ins(Mnemonic.Push, Rdx)
            .Cll("flush_stdout")
            .Ins(Mnemonic.Pop, Rdx)
            .Ins(Mnemonic.Pop, Rsi)
            .Mov(Rax, "SYS_WRITE")
            .Mov(Rdi, 1)
            .Syscall()
            .Ins(Mnemonic.Ret)
            .Lbl("print_decimal_skip_syscall")

            // rbx is number base
            // rax is number length (first R8)
            // rsi is stdout buffer
            // rdx is stdout buffer length
            .Mov(Rbx, Rsi)
            .Mov(R8, Rdx)
            .Lea(Rsi, "stdout_buffer")
            .Zro(Rdx)
            .Mov(Dx, new LabelAddress("stdout_len"))
            .Mov(Rax, _config.StdoutBufferSize)
            .Sub(Rax, Rdx) // remaining space
            .Cmp(Rax, R8)
            .Jg("print_decimal_skip_flush")
            .Cll("flush_stdout")
            .Lbl("print_decimal_skip_flush")
            .Add(Rsi, Rdx)
            .Mov(Rax, R8)
            .Zro(Rcx)
            .Lbl("print_decimal_loop2")
            .Mov(Dil, new Address(Rbx, Rcx))
            .Mov(new Address(Rsi, Rcx), Dil)
            .Inc(Rcx)
            .Cmp(Rcx, Rax)
            .Jne("print_decimal_loop2")
            .Add(Rdx, Rax)
            .Mov(new LabelAddress("stdout_len"), Dx)
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
            .Mov(R8B, 0)
            .Mov(R9B, 0)
            .Mov(new Register(ERegister.r10, ERegisterSize.l), -1)
            .Cmp(Dx, _config.StdoutBufferSize)
            .Ins(Mnemonic.CMovE, R8, R10);
        if (_config.FlushOnNewline)
        {
            _asm.Cmp(Dil, 10) // flush on newlines
                .Ins(Mnemonic.CMovE, R9, R10)
                .Or(R8B, R9B);
        }

        _asm.Cmp(R8B, 0)
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

    private void WritePrintString()
    {
        _asm.Str()
            .Com("Prints a string. fd in rdi, ptr in rsi, len in rdx.")
            .Lbl("print_string")
            .Ins(Mnemonic.Push, Rdi)
            .Ins(Mnemonic.Push, Rsi)
            .Ins(Mnemonic.Push, Rdx)
            .Cll("flush_stdout")
            .Ins(Mnemonic.Pop, Rdx)
            .Ins(Mnemonic.Pop, Rsi)
            .Ins(Mnemonic.Pop, Rdi)
            .Mov(Rax, "SYS_WRITE")
            .Syscall()
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

    private void PrintStringById(long id)
    {
        var stringLabel = GetStringLabel(id);
        var length = _program.Strings[id].Length;
        if (length == 0)
            return;
        if (length >= _config.StdoutBufferSize)
        {
            // Call write syscall directly instead of buffering string
            _asm.Cll("flush_stdout")
                .Mov(Rax, "SYS_WRITE")
                .Mov(Rdi, 1)
                .Lea(Rsi, stringLabel)
                .Mov(Rdx, length)
                .Syscall();
            return;
        }

        // if stdout buffer does not have space for string, flush.
        var skipFlush = GenerateNewLabel();
        _asm.Lea(Rsi, "stdout_buffer")
            .Zro(Rdx)
            .Mov(Dx, new LabelAddress("stdout_len"))
            .Mov(Rax, _config.StdoutBufferSize)
            .Sub(Rax, Rdx) // remaining space
            .Cmp(Rax, length)
            .Jg(skipFlush)
            .Cll("flush_stdout")
            .Lbl(skipFlush);

        // copy string to buffer.
        // rsi is stdout buffer + buffer length,
        // rdx is stdout buffer length
        // rbx is string buffer
        // rax is string length
        // rcx is counter
        // dil is current character
        var loop = GenerateNewLabel();
        _asm.Lea(Rbx, stringLabel)
            .Add(Rsi, Rdx)
            .Zro(Rcx)
            .Mov(Rax, length)
            .Lbl(loop)
            .Mov(Dil, new Address(Rbx, Rcx))
            .Mov(new Address(Rsi, Rcx), Dil)
            .Inc(Rcx)
            .Cmp(Rcx, Rax)
            .Jne(loop)
            .Add(Rdx, Rax)
            .Mov(new LabelAddress("stdout_len"), Dx);
    }

    private void PrintString(string strLabel, string lenLabel, int fd = 1)
    {
        _asm.Cll("flush_stdout")
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
        _asm.Cll("flush_stdout")
            .Mov(Rax, "SYS_EXIT")
            .Mov(Rdi, exitCode)
            .Syscall();
    }

    private void Push(Register register, StackElementType? type = default)
    {
        _asm.Mov(new Address(StackBase, StackCounter, Stride: 8), register);
        if (_config.TypeSafety is not TypeSafety.None)
        {
            if (type is not null)
                _asm.Mov(CurType, (long)type);
            _asm.Mov(new Address(TypeStackBase, StackCounter, Stride: 1), CurType);
        }

        _asm.Inc(StackCounter);
    }

    private void Replace(Register register, StackElementType? type = default)
    {
        _asm.Mov(new Address(StackBase, StackCounter, -1, 8), register);
        if (_config.TypeSafety is TypeSafety.None) return;
        if (type is not null)
            _asm.Mov(CurType, (long)type);
        _asm.Mov(new Address(TypeStackBase, StackCounter, -1), CurType);
    }

    private void Pop(Register register)
    {
        _asm.Dec(StackCounter)
            .Mov(register, new Address(StackBase, StackCounter, Stride: 8));

        if (_config.TypeSafety is not TypeSafety.None)
        {
            _asm.Mov(CurType, new Address(TypeStackBase, StackCounter, Stride: 1));
        }
    }

    private void Peek(Register register)
    {
        _asm.Mov(register, new Address(StackBase, StackCounter, -1, 8));

        if (_config.TypeSafety is not TypeSafety.None)
        {
            _asm.Mov(CurType, new Address(TypeStackBase, StackCounter, -1));
        }
    }

    private void VerifyPopped(StackElementType type)
    {
        if (_config.TypeSafety is TypeSafety.None ||
            _config.TypeSafety is TypeSafety.Lambda && type is not StackElementType.Lambda)
            return;
        var label = GenerateNewLabel();
        _asm.Cmp(CurType, (long)type)
            .Je(label)
            .Mov(Rdi, 2) // stderr
            .Lea(Rsi, $"err_msg_pop_{type}".ToLower())
            .Mov(Rdx, $"err_msg_pop_{type}_len".ToLower())
            .Cll("print_string");
        Exit(1);
        _asm.Lbl(label);
    }

    private void Pop(Register register, StackElementType type)
    {
        Pop(register);
        VerifyPopped(type);
    }

    private void Peek(Register register, StackElementType type)
    {
        Peek(register);
        VerifyPopped(type);
    }

    private void Drop()
    {
        _asm.Dec(StackCounter);
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
            .Str("type_references: RESB 32")
            .Str("string_buffer: RESB 32")
            .Str($"stdout_buffer: RESB {_config.StdoutBufferSize}");
    }

    private void WriteData()
    {
        _asm.Com("Globals:")
            .Sec(ESection.Data)
            .Str("stdout_len: DW 0");
    }

    private void WriteRoData()
    {
        _asm.Com("Constants:")
            .Sec(ESection.RoData);
        foreach (var entry in _program.Strings)
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