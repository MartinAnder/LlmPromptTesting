using Microsoft.Extensions.AI;
using Moq;
using Xunit;
using Xunit.Sdk;
using LlmPromptTesting;

namespace LlmPromptTesting.Tests.ChatResponseAssertionExtensions;

public class when_using_the_chat_response_extension
{
    private static Mock<IChatClient> CreateJudge(string verdict)
    {
        var mock = new Mock<IChatClient>();
        mock
            .Setup(j => j.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, verdict)));
        return mock;
    }

    [Fact]
    public async Task it_passes_when_the_judge_returns_yes()
    {
        // Arrange
        var judge = CreateJudge("{\"result\": true}");
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "I love you dearly."));

        // Act & Assert
        await response.ShouldSatisfyAsync(
            judge.Object,
            "Does this read like a love letter?",
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task it_throws_when_the_judge_returns_no()
    {
        // Arrange
        var judge = CreateJudge("{\"result\": false}");
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Q3 revenue increased."));

        // Act & Assert
        await Assert.ThrowsAsync<FailException>(
            () => response.ShouldSatisfyAsync(
                judge.Object,
                "Does this read like a love letter?",
                cancellationToken: TestContext.Current.CancellationToken));
    }
}
