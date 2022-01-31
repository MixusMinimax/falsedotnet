namespace FalseDotNet;

public interface ILogger
{
    public ILogger Write<T>(T message);
    public ILogger WriteLine<T>(T message);
}