using LlmPromptTesting.Anthropic;
using Microsoft.Extensions.AI;
using Xunit;
using Xunit.Sdk;

namespace LlmPromptTesting.IntegrationTests.LlmAssert;

public class when_judging_a_love_letter_response(AnthropicChatClientFixture fixture)
    : IClassFixture<AnthropicChatClientFixture>
{
    private const string ModelId = "claude-haiku-4-5-20251001";

    private readonly ChatOptions _options = new()
    {
        ModelId = ModelId,
        MaxOutputTokens = 1024,
    };

    [Fact]
    public async Task it_passes_when_the_criterion_matches()
    {
        // Arrange
        var response = await fixture.ChatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Write a short romantic love letter in 2-3 sentences.")],
            _options,
            TestContext.Current.CancellationToken);

        // Act & Assert
        await LlmPromptTesting.LlmAssert.JudgeAsync(
            fixture.ChatClient,
            response,
            "Does this read like a love letter?",
            ModelId,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task it_fails_when_the_criterion_does_not_match()
    {
        // Arrange
        var response = await fixture.ChatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "Write a short romantic love letter in 2-3 sentences.")],
            _options,
            TestContext.Current.CancellationToken);

        // Act & Assert
        await Assert.ThrowsAsync<FailException>(
            () => LlmPromptTesting.LlmAssert.JudgeAsync(
                fixture.ChatClient,
                response,
                "Does this read like a quarterly earnings report?",
                ModelId,
                TestContext.Current.CancellationToken));
    }
}
