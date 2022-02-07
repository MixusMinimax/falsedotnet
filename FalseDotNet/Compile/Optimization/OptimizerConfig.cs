namespace FalseDotNet.Compile.Optimization;

public class OptimizerConfig
{
    public enum EOptimizationLevel
    {
        /// <summary>
        /// No Optimization.
        /// </summary>
        O0,

        /// <summary>
        /// Remove redundant Instructions (repeated loads/stores)
        /// </summary>
        O1,

        /// <summary>
        /// Peephole Optimization, simplifying memory access.
        /// For example: If one FALSE command ends on a push, and the next one on a pop,
        /// these can be replaced with a simple register-to-register move.
        /// </summary>
        O2,
    }

    public EOptimizationLevel OptimizationLevel { get; set; }
}