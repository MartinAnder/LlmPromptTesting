using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace LlmPromptTesting.Tests.LlmAssert;

public class when_a_default_model_id_is_configured : IDisposable
{
    private readonly string? _previousDefaultModelId = LlmPromptTesting.LlmAssert.DefaultModelId;

    public void Dispose() => LlmPromptTesting.LlmAssert.DefaultModelId = _previousDefaultModelId;

    [Fact]
    public async Task it_uses_the_default_model_id_when_none_is_specified()
    {
        // Arrange
        var judge = new Mock<IChatClient>();
        ChatOptions? capturedOptions = null;
        judge
            .Setup(j => j.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>(
                (_, opts, _) => capturedOptions = opts)
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"result\": true}")));

        LlmPromptTesting.LlmAssert.DefaultModelId = "my-default-model";

        // Act
        await LlmPromptTesting.LlmAssert.JudgeAsync(
            judge.Object,
            text: "some text",
            criterion: "some criterion",
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("my-default-model", capturedOptions?.ModelId);
    }

    [Fact]
    public async Task it_uses_the_explicit_model_id_over_the_default()
    {
        // Arrange
        var judge = new Mock<IChatClient>();
        ChatOptions? capturedOptions = null;
        judge
            .Setup(j => j.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ChatMessage>, ChatOptions?, CancellationToken>(
                (_, opts, _) => capturedOptions = opts)
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"result\": true}")));

        LlmPromptTesting.LlmAssert.DefaultModelId = "my-default-model";

        // Act
        await LlmPromptTesting.LlmAssert.JudgeAsync(
            judge.Object,
            text: "some text",
            criterion: "some criterion",
            modelId: "explicit-model",
            cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("explicit-model", capturedOptions?.ModelId);
    }
}
