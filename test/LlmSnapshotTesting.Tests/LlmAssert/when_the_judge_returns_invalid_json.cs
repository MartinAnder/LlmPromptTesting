using Microsoft.Extensions.AI;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace LlmSnapshotTesting.Tests.LlmAssert;

public class when_the_judge_returns_invalid_json
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
    public async Task it_throws_xunit_exception_when_the_response_is_not_json()
    {
        // Arrange
        var judge = CreateJudge("yes");

        // Act & Assert
        await Assert.ThrowsAsync<FailException>(
            () => LlmSnapshotTesting.LlmAssert.JudgeAsync(judge.Object, "some text", "some criterion", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task it_throws_xunit_exception_when_the_result_field_is_missing()
    {
        // Arrange
        var judge = CreateJudge("{\"verdict\": true}");

        // Act & Assert
        await Assert.ThrowsAsync<FailException>(
            () => LlmSnapshotTesting.LlmAssert.JudgeAsync(judge.Object, "some text", "some criterion", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task it_throws_xunit_exception_when_the_result_field_is_not_a_boolean()
    {
        // Arrange
        var judge = CreateJudge("{\"result\": \"yes\"}");

        // Act & Assert
        await Assert.ThrowsAsync<FailException>(
            () => LlmSnapshotTesting.LlmAssert.JudgeAsync(judge.Object, "some text", "some criterion", cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task it_includes_the_received_response_in_the_exception_message()
    {
        // Arrange
        var invalidResponse = "not json at all";
        var judge = CreateJudge(invalidResponse);

        // Act
        var exception = await Assert.ThrowsAsync<FailException>(
            () => LlmSnapshotTesting.LlmAssert.JudgeAsync(judge.Object, "some text", "some criterion", cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(invalidResponse, exception.Message);
    }
}
