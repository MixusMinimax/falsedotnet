namespace FalseDotNet.Compilation;

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
}