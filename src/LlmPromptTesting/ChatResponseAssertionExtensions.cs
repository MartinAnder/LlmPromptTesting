using Microsoft.Extensions.AI;

namespace LlmPromptTesting;

public static class ChatResponseAssertionExtensions
{
    public static Task ShouldSatisfyAsync(
        this ChatResponse response,
        IChatClient judge,
        string criterion,
        string? modelId = null,
        CancellationToken cancellationToken = default
    )
        => LlmAssert.JudgeAsync(judge, response, criterion, modelId, cancellationToken);

    public static Task ShouldSatisfyAsync(
        this string text,
        IChatClient judge,
        string criterion,
        string? modelId = null,
        CancellationToken cancellationToken = default
    )
        => LlmAssert.JudgeAsync(judge, text, criterion, modelId, cancellationToken);
}
