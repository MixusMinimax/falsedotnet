using System.Drawing;
using FalseDotNet.Utility;
using Pastel;

namespace FalseDotNet.Compile.Optimization;

public class Optimizer : IOptimizer
{
    private readonly ILogger _logger;

    public Optimizer(ILogger logger)
    {
        _logger = logger;
    }

    public void Optimize(Asm asm, OptimizerConfig config)
    {
        // _logger.WriteLine(config.OptimizationLevel.ToString().Pastel(Color.Gold));
    }
}