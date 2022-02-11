namespace FalseDotNet.Utility;

public enum TypeSafety
{
    /// <summary>
    /// No Type safety.
    /// </summary>
    None,
    
    /// <summary>
    /// Only check for types with lambdas, since references are masked
    /// </summary>
    Lambda,
    
    /// <summary>
    /// In addition, disallow integers as references
    /// </summary>
    Full
}