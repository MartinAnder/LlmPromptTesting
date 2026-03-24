using LlmSnapshotTesting.Anthropic;
using Microsoft.Extensions.AI;
using Xunit;
using Xunit.Sdk;

namespace LlmSnapshotTesting.IntegrationTests.LlmAssert;

public class when_judging_a_technical_response(AnthropicChatClientFixture fixture)
    : IClassFixture<AnthropicChatClientFixture>
{
    private const string ModelId = "claude-haiku-4-5-20251001";

    private readonly ChatOptions _options = new()
    {
        ModelId = ModelId,
        MaxOutputTokens = 1024,
    };

    [Fact]
    public async Task it_passes_when_the_response_is_about_the_topic()
    {
        // Arrange
        var response = await fixture.ChatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Explain in one sentence what a binary search tree is.")],
            _options,
            TestContext.Current.CancellationToken);

        // Act & Assert
        await LlmSnapshotTesting.LlmAssert.JudgeAsync(
            fixture.ChatClient,
            response,
            "Does this response explain a computer science concept?",
            ModelId,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task it_fails_when_the_criterion_is_unrelated_to_the_response()
    {
        // Arrange
        var response = await fixture.ChatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Explain in one sentence what a binary search tree is.")],
            _options,
            TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<FailException>(
            () => LlmSnapshotTesting.LlmAssert.JudgeAsync(
                fixture.ChatClient,
                response,
                "Does this read like a love letter?",
                ModelId,
                TestContext.Current.CancellationToken));
    }
}
