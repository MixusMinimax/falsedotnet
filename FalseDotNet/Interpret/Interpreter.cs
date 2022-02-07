using System.Drawing;
using FalseDotNet.Instructions;
using FalseDotNet.Parse;
using FalseDotNet.Utility;
using Pastel;

namespace FalseDotNet.Interpret;

public class Interpreter : IInterpreter
{
    private readonly ILogger _logger;
    private readonly Stack<long> _stack = new();
    private readonly long[] _variables = new long[32];

    public Interpreter(ILogger logger)
    {
        _logger = logger;
    }

    private void Push(long value)
    {
        _stack.Push(value);
    }

    private void Push(bool value)
    {
        _stack.Push(value ? -1 : 0);
    }

    private long Peek() => _stack.Peek();

    private long Pop()
    {
        if (_stack.Count == 0)
            throw new InterpreterException("Tried to pop from empty stack!");
        return _stack.Pop();
    }

    public void Interpret(Program program, bool printOperations = true)
    {
        var (entryId, functions, strings) = program;
        var currentInstructions = functions[entryId];
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
            currentInstructions = functions[lambdaId];
        }

        for (var pc = 0; pc < currentInstructions.Count; ++pc)
        {
            var (operation, argument) = currentInstructions[pc];
            if (printOperations)
                _logger.WriteLine(currentInstructions[pc].ToString().Pastel(Color.FromArgb(255, 120, 120, 120)));
            long a, b;
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
                    (a, b) = (Pop(), Pop());
                    Push(a);
                    Push(b);
                    break;

                case Operation.Rot:
                    (var c, b, a) = (Pop(), Pop(), Pop());
                    Push(b);
                    Push(c);
                    Push(a);
                    break;

                case Operation.Pick:
                    a = Pop();
                    Push(_stack.ElementAt((int)a));
                    break;

                // Arithmetic

                case Operation.Add:
                    (a, b) = (Pop(), Pop());
                    Push(a + b);
                    break;

                case Operation.Sub:
                    (a, b) = (Pop(), Pop());
                    Push(b - a);
                    break;

                case Operation.Mul:
                    (a, b) = (Pop(), Pop());
                    Push(a * b);
                    break;

                case Operation.Div:
                    (a, b) = (Pop(), Pop());
                    Push(b / a);
                    break;

                case Operation.Neg:
                    a = Pop();
                    Push(-a);
                    break;

                case Operation.And:
                    (a, b) = (Pop(), Pop());
                    Push(a & b);
                    break;

                case Operation.Or:
                    (a, b) = (Pop(), Pop());
                    Push(a | b);
                    break;

                case Operation.Not:
                    a = Pop();
                    Push(~a);
                    break;

                // Comparison

                case Operation.Gt:
                    (a, b) = (Pop(), Pop());
                    Push(a < b);
                    break;

                case Operation.Eq:
                    (a, b) = (Pop(), Pop());
                    Push(a == b);
                    break;

                // Control Flow and Lambdas

                case Operation.Lambda:
                    Push(argument);
                    break;

                case Operation.Ret:
                    if (callStack.Count == 0)
                        throw new InterpreterException("Return from empty stack");
                    (pc, currentLambdaId) = callStack.Pop();
                    ValidateLambdaId(currentLambdaId, "callstack");
                    currentInstructions = functions[currentLambdaId];
                    break;

                case Operation.Execute:
                    lambdaId = Pop();
                    ExecuteLambda(lambdaId, ref pc);
                    break;

                case Operation.ConditionalExecute:
                    (lambdaId, condition) = (Pop(), Pop());
                    if (condition != 0) ExecuteLambda(lambdaId, ref pc);
                    break;

                case Operation.WhileInit:
                    // Pops the body and condition lambdas from the FALSE-stack and stores them on the callstack.
                    (body, condition) = (Pop(), Pop());
                    callStack.Push((0, condition));
                    callStack.Push((0, body));
                    break;

                case Operation.WhileCondition:
                    condition =  callStack.ElementAt(1).lambdaId;
                    ExecuteLambda(condition, ref pc);
                    break;

                case Operation.WhileBody:
                    condition = Pop();
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
                    // There is no character for inserting an exit instruction,
                    // it's automatically inserted of the main function.
                    // In the future, the exit code could be the top of the stack, or 0 if stack is empty.
                    return;

                // Names

                case Operation.Ref:
                    Push(argument);
                    break;

                case Operation.Store:
                    offset = Pop() % _variables.Length;
                    a = Pop();
                    _variables[offset] = a;
                    break;

                case Operation.Load:
                    offset = Pop() % _variables.Length;
                    Push(_variables[offset]);
                    break;

                // I/O

                case Operation.PrintString:
                    _logger.Write(strings[argument]);
                    break;
                
                case Operation.OutputChar:
                    a = Pop();
                    _logger.Write((char)a);
                    break;

                case Operation.OutputDecimal:
                    a = Pop();
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