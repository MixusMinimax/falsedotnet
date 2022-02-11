using System.Text;
using FalseDotNet.Utility;

namespace FalseDotNet.Cli;

public class DefaultLogger : IFlushableLogger
{
    private readonly List<byte> _buffer = new();

    private void WriteBuffer()
    {
        if (_buffer.Count is 0) return;
        Console.Write(Encoding.UTF8.GetString(_buffer.ToArray()));
        _buffer.Clear();
    }

    public ILogger Write<T>(T message)
    {
        switch (message)
        {
            case bool v:
                WriteBuffer();
                Console.Write(v);
                break;

            case char v:
                _buffer.Add(Convert.ToByte(v));
                break;

            case char[] bu:
                _buffer.AddRange(bu.Select(Convert.ToByte).ToArray());
                break;

            case double v:
                WriteBuffer();
                Console.Write(v);
                break;

            case decimal v:
                WriteBuffer();
                Console.Write(v);
                break;

            case float v:
                WriteBuffer();
                Console.Write(v);
                break;

            case int v:
                WriteBuffer();
                Console.Write(v);
                break;

            case uint v:
                WriteBuffer();
                Console.Write(v);
                break;

            case long v:
                WriteBuffer();
                Console.Write(v);
                break;

            case ulong v:
                WriteBuffer();
                Console.Write(v);
                break;

            default:
                WriteBuffer();
                Console.Write(message);
                break;
        }

        return this;
    }

    public ILogger WriteLine<T>(T message)
    {
        Write(message);
        Write('\n');
        return this;
    }

    public void Flush()
    {
        WriteBuffer();

        Console.Out.Flush();
    }
}