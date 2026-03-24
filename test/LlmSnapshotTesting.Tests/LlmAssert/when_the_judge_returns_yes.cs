using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace LlmSnapshotTesting.Tests.LlmAssert;

public class when_the_judge_returns_yes
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
    public async Task it_does_not_throw()
    {
        // Arrange
        var judge = CreateJudge("{\"result\": true}");

        // Act & Assert
        await LlmSnapshotTesting.LlmAssert.JudgeAsync(judge.Object, "some text", "some criterion", cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task it_does_not_throw_when_the_verdict_has_surrounding_whitespace()
    {
        // Arrange
        var judge = CreateJudge("  {\"result\": true}\n");

        // Act & Assert
        await LlmSnapshotTesting.LlmAssert.JudgeAsync(judge.Object, "some text", "some criterion", cancellationToken: TestContext.Current.CancellationToken);
    }
}
