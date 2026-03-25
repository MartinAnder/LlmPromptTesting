using Microsoft.Extensions.AI;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace LlmPromptTesting.Tests.LlmAssert;

public class when_the_judge_returns_no
{
    private const string Criterion = "Does this read like a love letter?";
    private const string Text = "quarterly earnings were up 12%";

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
    public async Task it_throws_xunit_exception()
    {
        // Arrange
        var judge = CreateJudge("{\"result\": false}");

        // Act & Assert
        await Assert.ThrowsAsync<FailException>(
            () => LlmPromptTesting.LlmAssert.JudgeAsync(judge.Object, Text, Criterion, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task it_includes_the_criterion_in_the_exception_message()
    {
        // Arrange
        var judge = CreateJudge("{\"result\": false}");

        // Act
        var exception = await Assert.ThrowsAsync<FailException>(
            () => LlmPromptTesting.LlmAssert.JudgeAsync(judge.Object, Text, Criterion, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(Criterion, exception.Message);
    }

    [Fact]
    public async Task it_includes_a_preview_of_the_text_in_the_exception_message()
    {
        // Arrange
        var judge = CreateJudge("{\"result\": false}");

        // Act
        var exception = await Assert.ThrowsAsync<FailException>(
            () => LlmPromptTesting.LlmAssert.JudgeAsync(judge.Object, Text, Criterion, cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(Text, exception.Message);
    }
}
