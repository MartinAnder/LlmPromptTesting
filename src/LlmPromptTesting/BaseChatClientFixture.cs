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
        var isCi = IsRunningInCi();
        var apiKey = apiKeyFactory();

        if (isCi)
        {
            if (apiKey is null)
                throw new InvalidOperationException("apikey for IChatClient must be set in CI.");

            ChatClient = chatClientFactory(apiKey);
        }
        else
        {
            IChatClient? innerChatClient = apiKey is not null
                ? chatClientFactory(apiKey)
                : null;

            ChatClient = new CachingChatClient(innerChatClient, GetSnapshotsDirectory());
        }
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

    protected static bool IsRunningInCi()
    {
        // Most CI platforms (GitHub Actions, GitLab CI, CircleCI, Travis CI, Bitbucket Pipelines, AppVeyor)
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
            return true;

        // Azure Pipelines
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD")))
            return true;

        // Jenkins
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("JENKINS_URL")))
            return true;

        // TeamCity
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TEAMCITY_VERSION")))
            return true;

        // AWS CodeBuild
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CODEBUILD_BUILD_ID")))
            return true;

        return false;
    }
}
