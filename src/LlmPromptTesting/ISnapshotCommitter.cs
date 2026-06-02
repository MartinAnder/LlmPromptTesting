namespace LlmPromptTesting;

/// <summary>
/// Commits a freshly recorded snapshot file to source control so it is
/// not accidentally left out of a commit before being pushed.
/// </summary>
public interface ISnapshotCommitter
{
    /// <summary>
    /// Stages and commits the snapshot at <paramref name="snapshotPath"/>.
    /// Implementations are best-effort: a failure to commit must not
    /// surface to the caller, because the snapshot has already been
    /// written to disk.
    /// </summary>
    /// <param name="snapshotPath">Absolute path to the snapshot file.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task CommitAsync(
        string snapshotPath,
        CancellationToken cancellationToken = default
    );
}
