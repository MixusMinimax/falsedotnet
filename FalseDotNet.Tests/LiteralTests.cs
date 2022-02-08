using FalseDotNet.Compile.Instructions;
using FluentAssertions;
using Xunit;

namespace FalseDotNet.Tests;

public class LiteralTests
{
    [Fact]
    public void ToString_ReturnCorrectValue_WhenL()
    {
        var a = new Literal(0, Size: ERegisterSize.l);
        a.ToString().Should().BeEquivalentTo("0x00");
        a = new Literal(1, Size: ERegisterSize.l);
        a.ToString().Should().BeEquivalentTo("0x01");
        a = new Literal(16, Size: ERegisterSize.l);
        a.ToString().Should().BeEquivalentTo("0x10");
        a = new Literal(-1, Size: ERegisterSize.l);
        a.ToString().Should().BeEquivalentTo("0xff");
    }
    
    [Fact]
    public void ToString_ReturnCorrectValue_WhenW()
    {
        var a = new Literal(0, Size: ERegisterSize.w);
        a.ToString().Should().BeEquivalentTo("0x00");
        a = new Literal(1, Size: ERegisterSize.w);
        a.ToString().Should().BeEquivalentTo("0x01");
        a = new Literal(16, Size: ERegisterSize.w);
        a.ToString().Should().BeEquivalentTo("0x10");
        a = new Literal(-1, Size: ERegisterSize.w);
        a.ToString().Should().BeEquivalentTo("0xffff");
    }
    
    [Fact]
    public void ToString_ReturnCorrectValue_WhenE()
    {
        var a = new Literal(0, Size: ERegisterSize.e);
        a.ToString().Should().BeEquivalentTo("0x00");
        a = new Literal(1, Size: ERegisterSize.e);
        a.ToString().Should().BeEquivalentTo("0x01");
        a = new Literal(16, Size: ERegisterSize.e);
        a.ToString().Should().BeEquivalentTo("0x10");
        a = new Literal(0x12345, Size: ERegisterSize.e);
        a.ToString().Should().BeEquivalentTo("0x012345");
        a = new Literal(-1, Size: ERegisterSize.e);
        a.ToString().Should().BeEquivalentTo("0xffffffff");
    }
    
    [Fact]
    public void ToString_ReturnCorrectValue_WhenR()
    {
        var a = new Literal(0, Size: ERegisterSize.r);
        a.ToString().Should().BeEquivalentTo("0x00");
        a = new Literal(1, Size: ERegisterSize.r);
        a.ToString().Should().BeEquivalentTo("0x01");
        a = new Literal(16, Size: ERegisterSize.r);
        a.ToString().Should().BeEquivalentTo("0x10");
        a = new Literal(0x12345, Size: ERegisterSize.e);
        a.ToString().Should().BeEquivalentTo("0x012345");
        a = new Literal(-1, Size: ERegisterSize.r);
        a.ToString().Should().BeEquivalentTo("0xffffffffffffffff");
    }
}