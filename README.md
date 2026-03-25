# LlmPromptTesting

Record and replay LLM responses in xUnit v3 tests. Captures live `IChatClient` responses as snapshots and replays them locally — no API key needed after the first run.

## Why?

LLM-powered features are hard to test. Real API calls are slow, expensive, and non-deterministic. Mocking them throws away the very thing you want to verify: that your prompts actually produce useful output.

**LlmPromptTesting** solves this with snapshot testing for LLM responses:

1. **First run** — calls the real API, saves the response to a `.llm-cache/` directory as a JSON snapshot.
2. **Subsequent runs** — replays the cached response instantly, with no API key required.
3. **CI** — always calls the real API so snapshots stay fresh. Commit the cache to version control and local runs are free.

This gives you deterministic, fast, offline-capable tests that still validate real LLM output. It also ships an **LLM-as-a-judge** assertion (`LlmAssert.JudgeAsync`) so you can assert that responses meet human-readable criteria without brittle string matching.

## Installation

```bash
# Core package (works with any IChatClient)
dotnet add package LlmPromptTesting

# Anthropic convenience fixture
dotnet add package LlmPromptTesting.Anthropic
```

## Quick start

### 1. Create a test fixture

The fixture provides an `IChatClient` that automatically records and replays responses.

**Using Anthropic (Claude):**

```csharp
// The built-in AnthropicChatClientFixture reads ANTHROPIC_API_KEY
// from the environment and wires everything up for you.
[CollectionDefinition(nameof(LlmCollection))]
public class LlmCollection : ICollectionFixture<AnthropicChatClientFixture>;
```

**Using any other provider:**

Subclass `BaseChatClientFixture` and supply your own client factory:

```csharp
public class OpenAiChatClientFixture : BaseChatClientFixture
{
    public OpenAiChatClientFixture() : base(
        apiKeyFactory: () => Environment.GetEnvironmentVariable("OPENAI_API_KEY"),
        chatClientFactory: apiKey => new OpenAIClient(apiKey)
            .GetChatClient("gpt-4o")
            .AsIChatClient())
    {
    }
}
```

### 2. Write a test

```csharp
[Collection(nameof(LlmCollection))]
public class when_asking_for_a_haiku(AnthropicChatClientFixture fixture)
{
    [Fact]
    public async Task it_returns_a_haiku()
    {
        // Arrange
        var messages = new ChatMessage[]
        {
            new(ChatRole.User, "Write a haiku about testing software.")
        };

        var options = new ChatOptions
        {
            ModelId = "claude-haiku-4-5-20251001"
        };

        // Act
        var response = await fixture.ChatClient.GetResponseAsync(
            messages,
            options,
            TestContext.Current.CancellationToken);

        // Assert — use an LLM judge instead of brittle string matching
        await LlmAssert.JudgeAsync(
            fixture.ChatClient,
            response,
            "Is this a valid haiku (three lines, 5-7-5 syllable pattern)?",
            "claude-haiku-4-5-20251001");
    }
}
```

The first time this test runs, it calls Claude, saves the response to `.llm-cache/`, and evaluates it. Every subsequent run replays the cached response — no network, no cost, same result.

### 3. Commit the cache

```bash
git add .llm-cache/
git commit -m "Add LLM response snapshots"
```

Now every developer on the team can run the tests without an API key.

## LLM-as-a-judge assertions

`LlmAssert.JudgeAsync` lets you assert that text satisfies a criterion, judged by an LLM. This replaces fragile regex or substring checks with natural-language criteria:

```csharp
// Assert against a ChatResponse
await LlmAssert.JudgeAsync(
    judge: fixture.ChatClient,
    response: chatResponse,
    criterion: "Does the response include a numbered list of at least 3 items?",
    modelId: "claude-haiku-4-5-20251001");

// Assert against raw text
await LlmAssert.JudgeAsync(
    judge: fixture.ChatClient,
    text: "The quick brown fox jumps over the lazy dog.",
    criterion: "Does this sentence contain every letter of the English alphabet?",
    modelId: "claude-haiku-4-5-20251001");
```

### Fluent syntax

Extension methods provide a more readable alternative:

```csharp
await response.ShouldSatisfyAsync(
    fixture.ChatClient,
    "Does the response read like a professional email?",
    "claude-haiku-4-5-20251001");

await "Hello, world!".ShouldSatisfyAsync(
    fixture.ChatClient,
    "Is this a greeting?",
    "claude-haiku-4-5-20251001");
```

### Default model

Set a default model to avoid repeating the model ID in every assertion:

```csharp
LlmAssert.DefaultModelId = "claude-haiku-4-5-20251001";

// Now you can omit the modelId parameter
await LlmAssert.JudgeAsync(
    fixture.ChatClient,
    response,
    "Does this answer the user's question?");
```

## How caching works

| Scenario | API key available? | Cache exists? | Behavior |
|---|---|---|---|
| Local dev | Yes | No | Calls API, saves snapshot |
| Local dev | Yes | Yes | Returns cached response |
| Local dev | No | Yes | Returns cached response |
| Local dev | No | No | Test is skipped |
| CI | Yes | — | Always calls API, saves snapshot |

Cache keys are SHA-256 hashes of the system instructions, messages, and model ID. Changing any of these invalidates the cache and triggers a fresh API call.

Snapshots are stored at `.llm-cache/{TestClass}/{TestMethod}_{hash}.json`.

## CI detection

The package automatically detects these CI environments: GitHub Actions, GitLab CI, CircleCI, Travis CI, Bitbucket Pipelines, AppVeyor, Azure Pipelines, Jenkins, TeamCity, and AWS CodeBuild.

In CI, the live `IChatClient` is always used (no caching layer), so your tests validate against real, up-to-date LLM output.
