using System;
using System.Diagnostics.CodeAnalysis;
using FalseDotNet.Compile.Instructions;
using FluentAssertions;
using Xunit;

namespace FalseDotNet.Tests;

public class AddressTests
{
    [Fact]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public void Disallow_SmallerThan32()
    {
        ((Action)(() => new Address(new Register(ERegister.ax, ERegisterSize.w))))
            .Should().Throw<ArgumentException>();
        ((Action)(() => new Address(new Register(ERegister.ax, ERegisterSize.h))))
            .Should().Throw<ArgumentException>();
        ((Action)(() => new Address(new Register(ERegister.ax, ERegisterSize.l))))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public void Disallow_DifferentSizes()
    {
        ((Action)(() => new Address(
                new Register(ERegister.ax, ERegisterSize.r),
                new Register(ERegister.bx, ERegisterSize.e))))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public void Disallow_ZeroStride()
    {
        ((Action)(() => new Address(
                new Register(ERegister.ax, ERegisterSize.l),
                new Register(ERegister.bx, ERegisterSize.r),
                0, 0
            )))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToString_OnlyBase()
    {
        var address = new Address
        (
            new Register(ERegister.bx, ERegisterSize.e)
        );
        address.ToString().Should().Be("[ebx]");
    }

    [Fact]
    public void ToString_OnlyBaseAndIndex()
    {
        var address = new Address
        (
            new Register(ERegister.bx, ERegisterSize.e),
            new Register(ERegister.cx, ERegisterSize.e)
        );
        address.ToString().Should().Be("[ebx+ecx]");
    }

    [Fact]
    public void ToString_OnlyBaseAndOffset()
    {
        var address = new Address
        (
            new Register(ERegister.sp, ERegisterSize.r),
            AddressOffset: 8
        );
        address.ToString().Should().Be("[rsp+8]");
    }

    [Fact]
    public void ToString_AllProperties()
    {
        var address = new Address
        (
            new Register(ERegister.bx, ERegisterSize.r),
            new Register(ERegister.cx, ERegisterSize.r),
            1,
            2,
            8
        );
        address.ToString().Should().Be("[rbx+(rcx+1)*2+8]");
    }
}