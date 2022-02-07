namespace FalseDotNet.Compile;

public class CompilerConfig
{
    /// <summary>
    /// Names of the entrypoint labels. "main" is the standard gcc entrypoint.
    /// </summary>
    public List<string> StartLabels { get; set; } = new() { "_start" };
    
    /// <summary>
    /// If true, insert the Operations as comments into the assembly code.
    /// </summary>
    public bool WriteInstructionComments { get; set; } = true;

    /// <summary>
    /// Size of the FALSE stack. Grows downwards.
    /// </summary>
    public long StackSize { get; set; } = 65_536;

    /// <summary>
    /// The size of the buffer used for number-to-string conversion.
    /// </summary>
    public long StringBufferSize { get; set; } = 32;

    /// <summary>
    /// The size of the buffer used for stdout buffering.
    /// </summary>
    public long StdoutBufferSize { get; set; } = 128;
}