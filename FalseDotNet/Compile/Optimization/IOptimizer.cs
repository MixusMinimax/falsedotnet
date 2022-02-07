namespace FalseDotNet.Compile.Optimization;

public interface IOptimizer
{
    public void Optimize(Assembly assembly, OptimizerConfig config);
}