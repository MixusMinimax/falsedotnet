using System.Drawing;
using FalseDotNet.Commands;
using FalseDotNet.Parse;
using FalseDotNet.Utility;
using Pastel;

namespace FalseDotNet.Interpret;

public class Interpreter : IInterpreter
{
    private readonly ILogger _logger;
    private readonly Stack<long> _stack = new();
    private readonly Stack<StackElementType> _types = new();
    private readonly long[] _variables = new long[32];
    private readonly StackElementType[] _varTypes = new StackElementType[32];

    public Interpreter(ILogger logger)
    {
        _logger = logger;
    }

    private void Push(long value, StackElementType type = StackElementType.Number)
    {
        if (type is StackElementType.Any)
            throw new InterpreterException($"Can not push type {type} on the stack!");
        _stack.Push(value);
        _types.Push(type);
    }

    private void Push((long Value, StackElementType Type) element)
    {
        var (value, type) = element;
        _stack.Push(value);
        _types.Push(type);
    }

    private void Push(bool value)
    {
        Push(value ? -1 : 0);
    }

    private (long Value, StackElementType Type) Peek(StackElementType type = StackElementType.Any)
    {
        if (_stack.Count == 0)
            throw new InterpreterException("Tried to pop from empty stack!");
        if (type is not StackElementType.Any && _types.Peek() != type)
            throw new InterpreterException($"Tried to pop {type} from stack, but top was {_types.Peek()}!");
        return (_stack.Peek(), _types.Peek());
    }

    private (long Value, StackElementType Type) Pop(StackElementType type = StackElementType.Any)
    {
        if (_stack.Count == 0)
            throw new InterpreterException("Tried to pop from empty stack!");
        var actual = _types.Pop();
        if (type is not StackElementType.Any && actual != type)
            throw new InterpreterException($"Tried to pop {type} from stack, but top was {actual}!");
        return (_stack.Pop(), actual);
    }

    public void Interpret(Program program, bool printOperations = false)
    {
        var (entryId, functions, strings) = program;
        var currentCommands = functions[entryId];
        long currentLambdaId = 0;
        var callStack = new Stack<(int ProgramCounter, long lambdaId)>();

        void ValidateLambdaId(long id, string where = "stack")
        {
            if (!functions.ContainsKey(id))
                throw new InterpreterException($"Invalid lambda id on {where}");
        }

        void ExecuteLambda(long lambdaId, ref int pc)
        {
            ValidateLambdaId(lambdaId);
            callStack.Push((pc, currentLambdaId));
            pc = -1;
            currentLambdaId = lambdaId;
            currentCommands = functions[lambdaId];
        }

        for (var pc = 0; pc < currentCommands.Count; ++pc)
        {
            var (operation, argument) = currentCommands[pc];
            if (printOperations)
                _logger.WriteLine(currentCommands[pc].ToString().Pastel(Color.FromArgb(255, 120, 120, 120)));
            long a, b;
            StackElementType ta, tb;
            long offset, condition;
            long lambdaId, body;
            switch (operation)
            {
                // Literals

                case Operation.IntLiteral:
                    Push(argument);
                    break;

                // Stack

                case Operation.Dup:
                    Push(Peek());
                    break;

                case Operation.Drop:
                    Pop();
                    break;

                case Operation.Swap:
                    ((a, ta), (b, tb)) = (Pop(), Pop());
                    Push(a, ta);
                    Push(b, tb);
                    break;

                case Operation.Rot:
                    (var (c, tc), (b, tb), (a, ta)) = (Pop(), Pop(), Pop());
                    Push(b, tb);
                    Push(c, tc);
                    Push(a, ta);
                    break;

                case Operation.Pick:
                    a = Pop(StackElementType.Number).Value;
                    Push(_stack.ElementAt((int)a), _types.ElementAt((int)a));
                    break;

                // Arithmetic

                case Operation.Add:
                    (a, b) = (Pop(StackElementType.Number).Value, Pop(StackElementType.Number).Value);
                    Push(a + b);
                    break;

                case Operation.Sub:
                    (a, b) = (Pop(StackElementType.Number).Value, Pop(StackElementType.Number).Value);
                    Push(b - a);
                    break;

                case Operation.Mul:
                    (a, b) = (Pop(StackElementType.Number).Value, Pop(StackElementType.Number).Value);
                    Push(a * b);
                    break;

                case Operation.Div:
                    (a, b) = (Pop(StackElementType.Number).Value, Pop(StackElementType.Number).Value);
                    Push(b / a);
                    break;

                case Operation.Neg:
                    a = Pop(StackElementType.Number).Value;
                    Push(-a);
                    break;

                case Operation.And:
                    (a, b) = (Pop(StackElementType.Number).Value, Pop(StackElementType.Number).Value);
                    Push(a & b);
                    break;

                case Operation.Or:
                    (a, b) = (Pop(StackElementType.Number).Value, Pop(StackElementType.Number).Value);
                    Push(a | b);
                    break;

                case Operation.Not:
                    a = Pop(StackElementType.Number).Value;
                    Push(~a);
                    break;

                // Comparison

                case Operation.Gt:
                    (a, b) = (Pop(StackElementType.Number).Value, Pop(StackElementType.Number).Value);
                    Push(a < b);
                    break;

                case Operation.Eq:
                    (a, b) = (Pop(StackElementType.Number).Value, Pop(StackElementType.Number).Value);
                    Push(a == b);
                    break;

                // Control Flow and Lambdas

                case Operation.Lambda:
                    Push(argument, StackElementType.Lambda);
                    break;

                case Operation.Ret:
                    if (callStack.Count == 0)
                        throw new InterpreterException("Return from empty stack");
                    (pc, currentLambdaId) = callStack.Pop();
                    ValidateLambdaId(currentLambdaId, "callstack");
                    currentCommands = functions[currentLambdaId];
                    break;

                case Operation.Execute:
                    lambdaId = Pop(StackElementType.Lambda).Value;
                    ExecuteLambda(lambdaId, ref pc);
                    break;

                case Operation.ConditionalExecute:
                    (lambdaId, condition) = (Pop(StackElementType.Lambda).Value, Pop(StackElementType.Number).Value);
                    if (condition != 0) ExecuteLambda(lambdaId, ref pc);
                    break;

                case Operation.WhileInit:
                    // Pops the body and condition lambdas from the FALSE-stack and stores them on the callstack.
                    ((body, _), (condition, _)) = (Pop(StackElementType.Lambda), Pop(StackElementType.Lambda));
                    callStack.Push((0, condition));
                    callStack.Push((0, body));
                    break;

                case Operation.WhileCondition:
                    condition = callStack.ElementAt(1).lambdaId;
                    ExecuteLambda(condition, ref pc);
                    break;

                case Operation.WhileBody:
                    (condition, _) = Pop(StackElementType.Number);
                    if (condition != 0)
                    {
                        body = callStack.Peek().lambdaId;
                        pc -= 2;
                        ExecuteLambda(body, ref pc);
                    }
                    else
                    {
                        callStack.Pop();
                        callStack.Pop();
                    }

                    break;

                case Operation.Exit:
                    // Not relevant for the interpreter.
                    // There is no character for inserting an exit command,
                    // it's automatically inserted at the end of the main function.
                    // In the future, the exit code could be the top of the stack, or 0 if stack is empty.
                    return;

                // Names

                case Operation.Ref:
                    Push(argument, StackElementType.Reference);
                    break;

                case Operation.Store:
                    offset = Pop(StackElementType.Reference).Value % _variables.Length;
                    (a, ta) = Pop();
                    _variables[offset] = a;
                    _varTypes[offset] = ta;
                    break;

                case Operation.Load:
                    offset = Pop(StackElementType.Reference).Value % _variables.Length;
                    Push(_variables[offset], _varTypes[offset]);
                    break;

                // I/O

                case Operation.PrintString:
                    _logger.Write(strings[argument]);
                    break;

                case Operation.OutputChar:
                    a = Pop(StackElementType.Number).Value;
                    _logger.Write((char)a);
                    break;

                case Operation.OutputDecimal:
                    a = Pop(StackElementType.Number).Value;
                    _logger.Write(a);
                    break;

                case Operation.Flush:
                    (_logger as IFlushableLogger)?.Flush();
                    break;

                default:
                    throw new InterpreterException("Unreachable");
            }
        }
    }
}