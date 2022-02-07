namespace FalseDotNet.Compile.Optimization;

public interface IOptimizer
{
    public void Optimize(Asm asm, OptimizerConfig config);
}