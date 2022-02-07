using FalseDotNet.Utility;

namespace FalseDotNet.Cli;

public class DefaultLogger : IFlushableLogger
{
    public ILogger Write<T>(T message)
    {
        switch (message)
        {
            case bool v:
                Console.Write(v);
                break;

            case char v:
                Console.Write(v);
                break;

            case char[] bu:
                Console.Write(bu);
                break;

            case double v:
                Console.Write(v);
                break;

            case decimal v:
                Console.Write(v);
                break;

            case float v:
                Console.Write(v);
                break;

            case int v:
                Console.Write(v);
                break;

            case uint v:
                Console.Write(v);
                break;

            case long v:
                Console.Write(v);
                break;

            case ulong v:
                Console.Write(v);
                break;

            default:
                Console.Write(message);
                break;
        }

        return this;
    }

    public ILogger WriteLine<T>(T message)
    {
        Write(message);
        Console.WriteLine();
        return this;
    }

    public void Flush()
    {
        Console.Out.Flush();
    }
}