using System.IO;
using FalseDotNet.Binary;
using FluentAssertions;
using Xunit;

namespace FalseDotNet.Tests;

public class PathConverterTests
{
    private readonly PathConverter _sut = new();

    [Fact]
    public void ConvertToWsl_ReturnConvertedPath()
    {
        _sut
            .ConvertToWsl(new FileInfo(@"C:\Users\example\Workspace\file.txt")).ToString()
            .Should().Be("/mnt/c/Users/example/Workspace/file.txt");
    }
}
