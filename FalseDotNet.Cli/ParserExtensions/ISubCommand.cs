namespace FalseDotNet.Cli.ParserExtensions;

public interface ISubCommand<in TOptions>
{
    int Run(TOptions opts);
}