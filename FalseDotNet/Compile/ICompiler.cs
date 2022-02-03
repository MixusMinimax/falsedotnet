using FalseDotNet.Parse;

namespace FalseDotNet.Compile;

public interface ICompiler
{
    public void Compile(Program program, TextWriter output);
}