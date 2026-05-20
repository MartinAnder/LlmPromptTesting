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

        ChatClient = new CachingChatClient(innerChatClient, GetSnapshotsDirectory());
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
    {
        var value = Environment.GetEnvironmentVariable("LLM_PROMPT_TESTING_FORCE_API");

        if (string.IsNullOrEmpty(value))
            return false;

        return value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value == "1";
    }
}
