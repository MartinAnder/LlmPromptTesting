using Microsoft.Extensions.AI;

namespace LlmPromptTesting;

public abstract class BaseChatClientFixture
{
    public IChatClient ChatClient { get; }

    protected BaseChatClientFixture(
        Func<string?> apiKeyFactory,
        Func<string, IChatClient> chatClientFactory
    )
    {
        if (IsReplayOnly() && (IsForcingApi() || IsCommittingMissingSnapshots()))
        {
            throw new InvalidOperationException(
                "LLM_PROMPT_TESTING_REPLAY_ONLY cannot be combined with "
                + "LLM_PROMPT_TESTING_FORCE_API or "
                + "LLM_PROMPT_TESTING_COMMIT_MISSING_SNAPSHOTS: replay-only "
                + "mode never records, so there is nothing to force or commit."
            );
        }

        if (IsReplayOnly())
        {
            ChatClient = new CachingChatClient(
                innerClient: null,
                GetSnapshotsDirectory(),
                replayOnly: true
            );
            return;
        }

        var apiKey = apiKeyFactory();

        if (IsForcingApi())
        {
            if (apiKey is null)
            {
                throw new InvalidOperationException(
                    "An API key must be set when LLM_PROMPT_TESTING_FORCE_API is enabled."
                );
            }

            ChatClient = chatClientFactory(apiKey);
            return;
        }

        IChatClient? innerChatClient = apiKey is not null
            ? chatClientFactory(apiKey)
            : null;

        ChatClient = new CachingChatClient(
            innerChatClient,
            GetSnapshotsDirectory(),
            snapshotCommitter: IsCommittingMissingSnapshots()
                ? new GitSnapshotCommitter()
                : null
        );
    }

    protected static string GetProjectRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        while (dir != null)
        {
            if (dir.GetFiles("*.csproj").Length > 0)
                return dir.FullName;

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate a .csproj file by walking up from the test output directory."
        );
    }

    protected static string GetSnapshotsDirectory()
        => Path.Combine(GetProjectRoot(), ".llm-cache");

    protected static bool IsForcingApi()
        => IsEnabled("LLM_PROMPT_TESTING_FORCE_API");

    protected static bool IsReplayOnly()
        => IsEnabled("LLM_PROMPT_TESTING_REPLAY_ONLY");

    protected static bool IsCommittingMissingSnapshots()
        => IsEnabled("LLM_PROMPT_TESTING_COMMIT_MISSING_SNAPSHOTS");

    private static bool IsEnabled(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrEmpty(value))
            return false;

        return value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value == "1";
    }
}
