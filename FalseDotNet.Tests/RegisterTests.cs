using System;
using System.Diagnostics.CodeAnalysis;
using FalseDotNet.Compile.Instructions;
using Xunit;
using FluentAssertions;

namespace FalseDotNet.Tests;

public class RegisterTests
{
    [Fact]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public void DisallowsHighByte_WhenRsiToR15()
    {
        ((Action)(() => new Register(ERegister.si, ERegisterSize.h))).Should().Throw<Exception>();
        ((Action)(() => new Register(ERegister.di, ERegisterSize.h))).Should().Throw<Exception>();
        ((Action)(() => new Register(ERegister.bp, ERegisterSize.h))).Should().Throw<Exception>();
        ((Action)(() => new Register(ERegister.sp, ERegisterSize.h))).Should().Throw<Exception>();
        ((Action)(() => new Register(ERegister.r8, ERegisterSize.h))).Should().Throw<Exception>();
        ((Action)(() => new Register(ERegister.r9, ERegisterSize.h))).Should().Throw<Exception>();
        ((Action)(() => new Register(ERegister.r10, ERegisterSize.h))).Should().Throw<Exception>();
        ((Action)(() => new Register(ERegister.r11, ERegisterSize.h))).Should().Throw<Exception>();
        ((Action)(() => new Register(ERegister.r12, ERegisterSize.h))).Should().Throw<Exception>();
        ((Action)(() => new Register(ERegister.r13, ERegisterSize.h))).Should().Throw<Exception>();
        ((Action)(() => new Register(ERegister.r14, ERegisterSize.h))).Should().Throw<Exception>();
        ((Action)(() => new Register(ERegister.r15, ERegisterSize.h))).Should().Throw<Exception>();
    }

    [Fact]
    [SuppressMessage("ReSharper", "ObjectCreationAsStatement")]
    public void AllowsHighByte_WhenAxToSp()
    {
        ((Action)(() => new Register(ERegister.ax, ERegisterSize.h))).Should().NotThrow();
        ((Action)(() => new Register(ERegister.bx, ERegisterSize.h))).Should().NotThrow();
        ((Action)(() => new Register(ERegister.cx, ERegisterSize.h))).Should().NotThrow();
        ((Action)(() => new Register(ERegister.dx, ERegisterSize.h))).Should().NotThrow();
    }

    [Fact]
    public void ToString_ReturnCorrectValue1()
    {
        var register = new Register(ERegister.ax, ERegisterSize.r);
        register.ToString().Should().Be("rax");
        register = new Register(ERegister.ax, ERegisterSize.e);
        register.ToString().Should().Be("eax");
        register = new Register(ERegister.ax, ERegisterSize.w);
        register.ToString().Should().Be("ax");
        register = new Register(ERegister.ax, ERegisterSize.h);
        register.ToString().Should().Be("ah");
        register = new Register(ERegister.ax, ERegisterSize.l);
        register.ToString().Should().Be("al");
    }

    [Fact]
    public void ToString_ReturnCorrectValue2()
    {
        var register = new Register(ERegister.si, ERegisterSize.r);
        register.ToString().Should().Be("rsi");
        register = new Register(ERegister.si, ERegisterSize.e);
        register.ToString().Should().Be("esi");
        register = new Register(ERegister.si, ERegisterSize.w);
        register.ToString().Should().Be("si");
        register = new Register(ERegister.si, ERegisterSize.l);
        register.ToString().Should().Be("sil");
    }

    [Fact]
    public void ToString_ReturnCorrectValue3()
    {
        var register = new Register(ERegister.r9, ERegisterSize.r);
        register.ToString().Should().Be("r9");
        register = new Register(ERegister.r9, ERegisterSize.e);
        register.ToString().Should().Be("r9d");
        register = new Register(ERegister.r9, ERegisterSize.w);
        register.ToString().Should().Be("r9w");
        register = new Register(ERegister.r9, ERegisterSize.l);
        register.ToString().Should().Be("r9b");
    }
}