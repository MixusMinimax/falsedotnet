using FalseDotNet.Compile.Optimization;
using FalseDotNet.Utility;

namespace FalseDotNet.Compile;

public record CompilerConfig
{
    /// <summary>
    /// Names of the entrypoint labels. "main" is the standard gcc entrypoint.
    /// </summary>
    public List<string> StartLabels { get; set; } = new() { "_start" };

    /// <summary>
    /// If true, insert the Operations as comments into the assembly code.
    /// </summary>
    public bool WriteCommandComments { get; set; } = true;

    /// <summary>
    /// Size of the FALSE stack.
    /// </summary>
    public long StackSize { get; set; } = 65_536;

    /// <summary>
    /// The size of the buffer used for number-to-string conversion.
    /// </summary>
    public long StringBufferSize { get; set; } = 32;

    /// <summary>
    /// The size of the buffer used for stdout buffering.
    /// </summary>
    public long StdoutBufferSize { get; set; } = 256;

    /// <summary>
    /// Automatically flush when \n is printed
    /// </summary>
    public bool FlushOnNewline { get; set; } = true;

    /// <summary>
    /// Which type safety level to use.
    /// When no type safety is selected, type information is not maintained at run-time.
    /// </summary>
    public TypeSafety TypeSafety { get; set; } = TypeSafety.None;

    public OptimizerConfig OptimizerConfig { get; set; } = new();
}