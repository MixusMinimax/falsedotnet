using FalseDotNet.Parsing;

namespace FalseDotNet.Compilation;

public interface ICompiler
{
    public void Compile(Program program, StreamWriter output);
}