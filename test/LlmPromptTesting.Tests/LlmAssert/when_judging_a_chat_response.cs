using Microsoft.Extensions.AI;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace LlmPromptTesting.Tests.LlmAssert;

public class when_judging_a_chat_response
{
    [Fact]
    public async Task it_passes_when_the_judge_approves_the_response_text()
    {
        // Arrange
        var judge = new Mock<IChatClient>();
        judge
            .Setup(j => j.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"result\": true}")));

        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "I love you dearly."));

        // Act & Assert
        await LlmPromptTesting.LlmAssert.JudgeAsync(
            judge.Object,
            response,
            "Does this read like a love letter?",
            cancellationToken: TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task it_throws_when_the_judge_rejects_the_response_text()
    {
        // Arrange
        var judge = new Mock<IChatClient>();
        judge
            .Setup(j => j.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"result\": false}")));

        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, "Q3 revenue increased."));

        // Act & Assert
        await Assert.ThrowsAsync<FailException>(
            () => LlmPromptTesting.LlmAssert.JudgeAsync(
                judge.Object,
                response,
                "Does this read like a love letter?",
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task it_sends_the_response_text_to_the_judge()
    {
        // Arrange
        var judge = new Mock<IChatClient>();
        judge
            .Setup(j => j.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"result\": true}")));

        var responseText = "I love you dearly.";
        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, responseText));

        // Act
        await LlmPromptTesting.LlmAssert.JudgeAsync(
            judge.Object,
            response,
            "some criterion",
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        judge.Verify(j => j.GetResponseAsync(
            It.Is<IEnumerable<ChatMessage>>(msgs =>
                msgs.Any(m => m.Text != null && m.Text.Contains(responseText))),
            It.IsAny<ChatOptions?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
