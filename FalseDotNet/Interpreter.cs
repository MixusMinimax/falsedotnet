using System.Drawing;
using FalseDotNet.Operations;
using Pastel;

namespace FalseDotNet;

public class Interpreter : IInterpreter
{
    private readonly ILogger _logger;
    private readonly Stack<object> _stack = new();
    private readonly object[] _variables = new object[32];

    public Interpreter(ILogger logger)
    {
        _logger = logger;
    }

    private void Push<T>(T t)
    {
        _stack.Push(t!);
    }

    private object Peek() => _stack.Peek();

    private object Pop() => Pop<object>();

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
            long a, b, c, offset;
            switch (operation)
            {
                case IntLiteralOp intLiteralOp:
                    Push(intLiteralOp.Value);
                    break;

                case DupOp:
                    Push(Peek());
                    break;

                case DropOp:
                    Pop();
                    break;

                case SwapOp:
                    (a, b) = (Pop<long>(), Pop<long>());
                    Push(a);
                    Push(b);
                    break;

                case RotOp:
                    (c, b, a) = (Pop<long>(), Pop<long>(), Pop<long>());
                    Push(b);
                    Push(c);
                    Push(a);
                    break;

                case PickOp:
                    a = Pop<long>();
                    Push(_stack.ElementAt((int)a));
                    break;

                case AddOp:
                    (a, b) = (Pop<long>(), Pop<long>());
                    Push(a + b);
                    break;

                case SubOp:
                    (a, b) = (Pop<long>(), Pop<long>());
                    Push(b - a);
                    break;

                case MulOp:
                    (a, b) = (Pop<long>(), Pop<long>());
                    Push(a * b);
                    break;

                case DivOp:
                    (a, b) = (Pop<long>(), Pop<long>());
                    Push(b / a);
                    break;

                case NegOp:
                    a = Pop<long>();
                    Push(-a);
                    break;

                case AndOp:
                    (a, b) = (Pop<long>(), Pop<long>());
                    Push(a & b);
                    break;

                case OrOp:
                    (a, b) = (Pop<long>(), Pop<long>());
                    Push(a | b);
                    break;

                case NotOp:
                    a = Pop<long>();
                    Push(~a);
                    break;

                case GtOp:
                    (a, b) = (Pop<long>(), Pop<long>());
                    Push(a < b);
                    break;

                case EqOp:
                    (a, b) = (Pop<long>(), Pop<long>());
                    Push(a == b);
                    break;

                case RefOp refOp:
                    Push(refOp.Index);
                    break;

                case StoreOp:
                    offset = Pop<long>() % _variables.Length;
                    a = Pop<long>();
                    _variables[offset] = a;
                    break;
                
                case LoadOp:
                    offset = Pop<long>() % _variables.Length;
                    Push(_variables[offset]);
                    break;

                case OutputCharOp:
                    a = Pop<long>();
                    _logger.Write((char)a);
                    break;

                case OutputDecimalOp:
                    a = Pop<long>();
                    _logger.Write(a);
                    break;
            }
        }
    }
}