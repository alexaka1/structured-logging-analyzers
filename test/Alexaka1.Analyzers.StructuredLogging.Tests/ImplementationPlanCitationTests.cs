using System.Diagnostics;

using Alexaka1.Analyzers.StructuredLogging.Tests.Infrastructure;

using Xunit;

namespace Alexaka1.Analyzers.StructuredLogging.Tests;

public sealed class ImplementationPlanCitationTests
{
    [Fact]
    public void Tracked_files_do_not_cite_the_implementation_plan_file()
    {
        var repoRoot = NuGetPackageResolver.FindRepoRoot();
        var citedName = string.Concat("PLAN", ".md");
        var hits = new List<string>();

        foreach (var relativePath in GitTrackedFiles(repoRoot))
        {
            if (IsImplementationPlanFile(relativePath, citedName))
            {
                continue;
            }

            var fullPath = Path.Combine(repoRoot, relativePath);
            if (!File.Exists(fullPath) || LooksBinary(fullPath, relativePath))
            {
                continue;
            }

            var text = File.ReadAllText(fullPath);
            var index = 0;
            while ((index = text.IndexOf(citedName, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                hits.Add($"{relativePath}:{LineNumber(text, index)}");
                index += citedName.Length;
            }
        }

        Assert.True(
            hits.Count == 0,
            "The implementation-plan scratch file is not documentation and must not be cited:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, hits));
    }

    private static bool IsImplementationPlanFile(string relativePath, string citedName)
    {
        var fileName = Path.GetFileName(relativePath.Replace('\\', '/'));
        return fileName.Equals(citedName, StringComparison.OrdinalIgnoreCase);
    }

    private static int LineNumber(string text, int index)
    {
        var line = 1;
        for (var i = 0; i < index; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    private static bool LooksBinary(string fullPath, string relativePath)
    {
        var extension = Path.GetExtension(relativePath);
        if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".ico", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".nupkg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".snupkg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".woff", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".woff2", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        using var stream = File.OpenRead(fullPath);
        var buffer = new byte[Math.Min(8192, stream.Length)];
        var read = stream.Read(buffer, 0, buffer.Length);
        return buffer.AsSpan(0, read).Contains((byte)0);
    }

    private static IEnumerable<string> GitTrackedFiles(string repoRoot)
    {
        var psi = new ProcessStartInfo("git", "ls-files -z")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start git.");
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        Task.WaitAll(stdoutTask, stderrTask);
        process.WaitForExit();
        Assert.True(process.ExitCode == 0, stderrTask.Result);
        var output = stdoutTask.Result;

        foreach (var path in output.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            yield return path.Replace('\\', '/');
        }
    }
}
