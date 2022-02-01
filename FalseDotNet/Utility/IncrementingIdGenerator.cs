namespace FalseDotNet.Utility;

public class IncrementingIdGenerator : IIdGenerator
{
    private long _current = 0;

    public long NewId => _current++;
}