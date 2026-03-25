using Anthropic.SDK;

namespace LlmPromptTesting.Anthropic;

public class AnthropicChatClientFixture : BaseChatClientFixture
{
    public AnthropicChatClientFixture() : base(
        () => Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY"),
        (apiKey) => new AnthropicClient(apiKey).Messages) { }
}

