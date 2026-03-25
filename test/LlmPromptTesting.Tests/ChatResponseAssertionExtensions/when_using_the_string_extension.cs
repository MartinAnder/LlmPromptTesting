using Microsoft.Extensions.AI;
using Moq;
using Xunit;
using Xunit.Sdk;
using LlmPromptTesting;

namespace LlmPromptTesting.Tests.ChatResponseAssertionExtensions;

public class when_using_the_string_extension
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

        // Act & Assert
        await "I love you dearly.".ShouldSatisfyAsync(
            judge.Object,
            "Does this read like a love letter?",
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task it_throws_when_the_judge_returns_no()
    {
        // Arrange
        var judge = CreateJudge("{\"result\": false}");

        // Act & Assert
        await Assert.ThrowsAsync<FailException>(
            () => "Q3 revenue increased.".ShouldSatisfyAsync(
                judge.Object,
                "Does this read like a love letter?",
                cancellationToken: TestContext.Current.CancellationToken));
    }
}
