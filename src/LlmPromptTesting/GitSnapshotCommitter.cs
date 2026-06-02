using System.Diagnostics;

namespace LlmPromptTesting;

/// <summary>
/// Stages and commits a snapshot file with <c>git</c>. Best-effort: if
/// git is unavailable, the directory is not a repository, or the commit
/// otherwise fails, the snapshot stays written to disk and no exception
/// is surfaced to the test.
/// </summary>
public sealed class GitSnapshotCommitter : ISnapshotCommitter
{
    /// <inheritdoc />
    public async Task CommitAsync(
        string snapshotPath,
        CancellationToken cancellationToken = default
    )
    {
        var workingDirectory = Path.GetDirectoryName(snapshotPath);
        if (workingDirectory is null)
        {
            return;
        }

        var fileName = Path.GetFileName(snapshotPath);

        // Best-effort source-control bookkeeping: committing a recorded
        // snapshot is a convenience, not part of recording's contract, so
        // a missing git binary or a non-repository directory must not fail
        // the test that just recorded successfully.
        try
        {
            await RunGitAsync(
                workingDirectory,
                cancellationToken,
                "add",
                "--",
                snapshotPath
            );

            await RunGitAsync(
                workingDirectory,
                cancellationToken,
                "commit",
                "-m",
                $"test: record LLM snapshot {fileName}",
                "--",
                snapshotPath
            );
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            // Swallowed deliberately — see the boundary comment above.
        }
    }

    private static async Task RunGitAsync(
        string workingDirectory,
        CancellationToken cancellationToken,
        params string[] arguments
    )
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("git could not be started.");

        await process.WaitForExitAsync(cancellationToken);
    }
}
