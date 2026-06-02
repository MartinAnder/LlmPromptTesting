namespace LlmPromptTesting;

/// <summary>
/// Thrown when a chat request has no recorded snapshot and replay-only
/// mode (<c>LLM_PROMPT_TESTING_REPLAY_ONLY</c>) is enabled, so no live
/// API call is attempted.
/// </summary>
public sealed class SnapshotNotFoundException : Exception
{
    /// <summary>The cache key computed for the unmatched request.</summary>
    public string CacheKey { get; }

    /// <summary>The snapshot file path that was expected to exist.</summary>
    public string SnapshotPath { get; }

    /// <summary>
    /// Initializes a new <see cref="SnapshotNotFoundException"/>.
    /// </summary>
    /// <param name="cacheKey">The cache key computed for the request.</param>
    /// <param name="snapshotPath">The snapshot file path that was expected.</param>
    public SnapshotNotFoundException(string cacheKey, string snapshotPath)
        : base(
            $"No recorded snapshot for cache key {cacheKey} "
            + $"(expected at {snapshotPath}). Replay-only mode "
            + "(LLM_PROMPT_TESTING_REPLAY_ONLY) is enabled, so no live API "
            + "call was made. Re-record by unsetting "
            + "LLM_PROMPT_TESTING_REPLAY_ONLY, setting the API key, and "
            + "re-running, then commit the updated snapshot."
        )
    {
        CacheKey = cacheKey;
        SnapshotPath = snapshotPath;
    }
}
