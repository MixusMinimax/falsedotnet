using System.Diagnostics;
using System.Drawing;
using FalseDotNet.Utility;
using Pastel;

namespace FalseDotNet.Binary;

public interface ILinuxExecutor
{
    public Task<int> ExecuteAsync(string fileName, string arguments);
    public Task<int> AssembleAsync(string inputPath, string outputPath);
    public Task<int> LinkAsync(string inputPath, string outputPath);
}

public class LinuxExecutor : ILinuxExecutor
{
    private readonly ILogger _logger;

    public LinuxExecutor(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<int> ExecuteAsync(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo();

        if (OperatingSystem.IsWindows())
        {
            startInfo.FileName = @"bash.exe";
            startInfo.Arguments =
                "-c " + $"{fileName} {arguments}".Escape();
        }
        else if (OperatingSystem.IsLinux())
        {
            startInfo.FileName = fileName;
            startInfo.Arguments = arguments;
        }
        else
        {
            throw new Exception("Operating System not supported!");
        }

        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        _logger.WriteLine($"Command: {startInfo.FileName} {startInfo.Arguments}".Pastel(Color.SlateGray));
        var process = new Process();
        process.StartInfo = startInfo;
        process.Start();
        
        if (OperatingSystem.IsWindows())
            _logger.Write('\r');

        await Task.WhenAll(((Func<Task>)(async () =>
        {
            string? s;
            while ((s = await process.StandardOutput.ReadLineAsync()) is not null)
            {
                _logger.Write(s);
                if (OperatingSystem.IsWindows())
                    _logger.Write('\r');
                _logger.Write('\n');
            }
        }))(), ((Func<Task>)(async () =>
        {
            string? s;
            while ((s = await process.StandardError.ReadLineAsync()) is not null)
            {
                _logger.WriteLine(s.Pastel(Color.IndianRed));
                if (OperatingSystem.IsWindows())
                    _logger.Write('\r');
                _logger.Write('\n');
            }
        }))());

        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
        {
            throw new Exception($"{fileName} failed! ExitCode: {process.ExitCode}");
        }

        return process.ExitCode;
    }

    public Task<int> AssembleAsync(string inputPath, string outputPath)
    {
        return ExecuteAsync("nasm", $"-felf64 -o \"{outputPath}\" \"{inputPath}\"");
    }

    public Task<int> LinkAsync(string inputPath, string outputPath)
    {
        return ExecuteAsync("ld", $"-o \"{outputPath}\" \"{inputPath}\"");
    }
}