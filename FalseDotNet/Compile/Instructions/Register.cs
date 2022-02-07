using System.Diagnostics.CodeAnalysis;

namespace FalseDotNet.Compile.Instructions;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum ERegister
{
    ax,
    bx,
    cx,
    dx,
    si,
    di,
    bp,
    sp,
    r8,
    r9,
    r10,
    r11,
    r12,
    r13,
    r14,
    r15
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum ERegisterSize
{
    l,
    h,
    w,
    e,
    r
}

public record Register : IOperand
{
    public readonly ERegister Name;
    public readonly ERegisterSize Size;

    public Register(ERegister name, ERegisterSize size)
    {
        Name = name;
        if (size is ERegisterSize.h && name is >= ERegister.si and <= ERegister.r15)
            throw new Exception("high does not exist for r8-r15");
        Size = size;
    }

    public override string ToString()
    {
        string Begin() => Size switch
        {
            ERegisterSize.l or ERegisterSize.h or ERegisterSize.w => "",
            _ => Size.ToString()
        };

        string XEnd() => Size switch
        {
            ERegisterSize.l => "l",
            ERegisterSize.h => "h",
            _ => "x"
        };

        string End() => Size switch
        {
            ERegisterSize.l => "l",
            ERegisterSize.h => throw new Exception("Unreachable"),
            _ => ""
        };

        string REnd() => Size switch
        {
            ERegisterSize.l => "b",
            ERegisterSize.h => throw new Exception("Unreachable"),
            ERegisterSize.w => "w",
            ERegisterSize.e => "d",
            _ => ""
        };

        return Name switch
        {
            >= ERegister.ax and <= ERegister.dx =>
                Begin() + Name.ToString()[..1] + XEnd(),

            >= ERegister.si and <= ERegister.sp =>
                Begin() + Name + End(),

            _ => Name + REnd()
        };
    }
}