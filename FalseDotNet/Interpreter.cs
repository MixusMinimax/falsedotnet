using System.Drawing;
using FalseDotNet.Operations;
using Pastel;

namespace FalseDotNet;

public class Interpreter : IInterpreter
{
    private readonly ILogger _logger;
    private readonly Stack<object> _stack = new();

    public Interpreter(ILogger logger)
    {
        _logger = logger;
    }

    private void Push<T>(T t)
    {
        _stack.Push(t!);
    }

    private T Pop<T>()
    {
        if (_stack.Count == 0)
            throw new InterpreterException("Tried to pop from empty stack!");
        if (_stack.Pop() is not T ret)
            throw new InterpreterException("Tried to pop incorrect type from stack!");
        return ret;
    }

    public void Interpret(IEnumerable<IOperation> operations, bool printOperations = true)
    {
        foreach (var operation in operations)
        {
            if (printOperations)
                _logger.WriteLine(operation.ToString().Pastel(Color.FromArgb(255, 120, 120, 120)));
            ulong ul;
            switch (operation)
            {
                case IntLiteralOp intLiteralOp:
                    _stack.Push(intLiteralOp.Value);
                    break;

                case AddOp:
                    var a = Pop<ulong>();
                    var b = Pop<ulong>();
                    _stack.Push(a + b);
                    break;

                case OutputCharOp:
                    ul = Pop<ulong>();
                    _logger.Write((char)ul);
                    break;

                case OutputDecimalOp:
                    ul = Pop<ulong>();
                    _logger.Write(ul);
                    break;
            }
        }
    }
}