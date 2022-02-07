using Microsoft.Extensions.DependencyInjection;

namespace FalseDotNet.Cli.SubCommandExtensions;

public interface ISubCommand
{
    int Run(object opts);
}

public abstract class SubCommand<TOptions> : ISubCommand
{
    public int Run(object opts)
    {
        return Run((TOptions)opts);
    }

    public abstract int Run(TOptions opts);
}