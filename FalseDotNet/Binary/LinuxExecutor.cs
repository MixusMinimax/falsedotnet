using System.Diagnostics;
using System.Drawing;
using FalseDotNet.Utility;
using Pastel;

namespace FalseDotNet.Binary;

public interface ILinuxExecutor
{
    public Task<int> ExecuteAsync(string fileName, string arguments, TextReader? input = null);
    public Task<int> AssembleAsync(FileInfo inputPath, FileInfo outputPath);
    public Task<int> LinkAsync(FileInfo inputPath, FileInfo outputPath);
}

public class LinuxExecutor(ILogger logger) : ILinuxExecutor
{
    public async Task<int> ExecuteAsync(string fileName, string arguments, TextReader? input = null)
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
        if (input is not null) startInfo.RedirectStandardInput = true;
        logger.WriteLine($"Command: {startInfo.FileName} {startInfo.Arguments}".Pastel(Color.SlateGray));
        var process = new Process();
        process.StartInfo = startInfo;
        process.Start();


        if (OperatingSystem.IsWindows())
            logger.Write('\r');

        await Task.WhenAll(((Func<Task>)(async () =>
        {
            string? s;
            while ((s = await process.StandardOutput.ReadLineAsync()) is not null)
            {
                logger.Write(s);
                if (OperatingSystem.IsWindows())
                    logger.Write('\r');
                logger.Write('\n');
            }
        }))(), ((Func<Task>)(async () =>
        {
            string? s;
            while ((s = await process.StandardError.ReadLineAsync()) is not null)
            {
                logger.WriteLine(s.Pastel(Color.IndianRed));
                if (OperatingSystem.IsWindows())
                    logger.Write('\r');
                logger.Write('\n');
            }
        }))(), ((Func<Task>)(async () =>
        {
            if (input is null) return;
            var mem = new char[16];
            var tokenFactory = new CancellationTokenSource();
            process.Exited += (_, _) => tokenFactory.Cancel();
            try
            {
                while (!process.HasExited)
                {
                    var res = await input.ReadAsync(mem, tokenFactory.Token);
                    if (res is not 0)
                        await process.StandardInput.WriteAsync(mem.AsMemory()[..res], tokenFactory.Token);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }))(), process.WaitForExitAsync());

        if (process.ExitCode != 0)
        {
            throw new Exception($"{fileName} failed! ExitCode: {process.ExitCode}");
        }

        return process.ExitCode;
    }

    public Task<int> AssembleAsync(FileInfo inputPath, FileInfo outputPath)
    {
        return ExecuteAsync("nasm", $"-felf64 -o \"{outputPath}\" \"{inputPath}\"");
    }

    public Task<int> LinkAsync(FileInfo inputPath, FileInfo outputPath)
    {
        return ExecuteAsync("ld", $"-o \"{outputPath}\" \"{inputPath}\"");
    }
}
