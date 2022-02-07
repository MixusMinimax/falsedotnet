using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FalseDotNet.Compile.Instructions;

public record Address(
    Register Base,
    Register? Index = null,
    long IndexOffset = 0,
    ulong Stride = 1,
    long AddressOffset = 0
) : IOperand
{
    public override string ToString()
    {
        var builder = new StringBuilder();
        builder.Append('[');
        builder.Append(Base);
        if (Index is not null)
        {
            builder.Append('+');
            if (IndexOffset is not 0)
                builder.Append('(');
            builder.Append(Index);
            if (IndexOffset is not 0)
            {
                builder.Append('+');
                builder.Append(IndexOffset);
                builder.Append(')');
            }

            if (Stride is not 1)
            {
                builder.Append('*');
                builder.Append(Stride);
            }
        }

        if (AddressOffset is not 0)
        {
            builder.Append('+');
            builder.Append(AddressOffset);
        }

        builder.Append(']');
        return builder.ToString();
    }

    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    private bool _dummy = Validate(Base, Index, IndexOffset, Stride, AddressOffset);

    [SuppressMessage("ReSharper", "ParameterOnlyUsedForPreconditionCheck.Local")]
    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private static bool Validate(Register @base, Register? index, long indexOffset, ulong stride, long addressOffset)
    {
        if (@base is { Size: < ERegisterSize.e })
            throw new ArgumentException("base needs to be at least 32 bit!");
        if (index is not null && index.Size != @base.Size)
            throw new ArgumentException("index and base need to be of the same size!");
        if (stride is 0)
            throw new ArgumentException("Stride can not be 0!");
        return true;
    }
}