using FalseDotNet.Binary;
using FluentAssertions;
using Xunit;

namespace FalseDotNet.Tests;

public class PathConverterTests
{
    private readonly PathConverter _sut;

    public PathConverterTests()
    {
        _sut = new PathConverter();
    }

    [Fact]
    public void ConvertToWsl_ReturnConvertedPath()
    {
        _sut
            .ConvertToWsl(@"C:\Users\example\Workspace\file.txt")
            .Should().Be("/mnt/c/Users/example/Workspace/file.txt");
    }
}