# LlmPromptTesting

Record and replay LLM responses in xUnit v3 tests. Captures live `IChatClient` responses as snapshots and replays them locally — no API key needed after the first run.

## Why?

LLM-powered features are hard to test. Real API calls are slow, expensive, and non-deterministic. Mocking them throws away the very thing you want to verify: that your prompts actually produce useful output.

**LlmPromptTesting** solves this with snapshot testing for LLM responses:

1. **First run** — calls the real API, saves the response to a `.llm-cache/` directory as a JSON snapshot.
2. **Subsequent runs** — replays the cached response instantly, with no API key required.
3. **CI** — replays from the committed cache by default, so PRs cost zero credits. Set `LLM_PROMPT_TESTING_FORCE_API=true` (with an API key) to refresh snapshots against the real API.

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

The same caching layer is used everywhere — including CI — so tests run against the committed `.llm-cache/` snapshots by default.

| `LLM_PROMPT_TESTING_FORCE_API` | API key available? | Cache exists? | Behavior |
|---|---|---|---|
| unset | Yes | Yes | Returns cached response |
| unset | Yes | No | Calls API, saves snapshot |
| unset | No | Yes | Returns cached response |
| unset | No | No | Test is skipped |
| `true` | Yes | — | Always calls API, overwrites snapshot |
| `true` | No | — | Throws — an API key is required |

Cache keys are SHA-256 hashes of the system instructions, messages, and model ID. Changing any of these invalidates the cache and triggers a fresh API call.

Snapshots are stored at `.llm-cache/{TestClass}/{TestMethod}_{hash}.json`.

Two optional environment variables tune this behavior further: `LLM_PROMPT_TESTING_REPLAY_ONLY` (a strict offline mode for CI) and `LLM_PROMPT_TESTING_COMMIT_MISSING_SNAPSHOTS` (auto-commit freshly recorded snapshots locally). Both are described below.

## Forcing real API calls

Set `LLM_PROMPT_TESTING_FORCE_API=true` (or `1`) to bypass the cache entirely and hit the live `IChatClient`. Use this when you intentionally want to re-record snapshots against the real API — for example, on a scheduled CI run or after a prompt change.

```bash
LLM_PROMPT_TESTING_FORCE_API=true ANTHROPIC_API_KEY=sk-... dotnet test
```

When the flag is not set, CI behaves exactly like local development: replays from cache, costs nothing in API credits, and only consumes credits if a key is present *and* a cache entry is missing.

## Strict replay-only mode

Set `LLM_PROMPT_TESTING_REPLAY_ONLY=true` (or `1`) to forbid live API calls entirely. On a cache **hit** the snapshot is replayed as usual; on a **miss** the call throws a `SnapshotNotFoundException` — a hard test failure — *before* any `IChatClient` is consulted, so no network request is made and no API key is needed.

This is the mode to set in CI. It guarantees a pull request can never spend API credits, and it turns a stale or missing snapshot into a loud red failure naming the cache key, instead of a silent live call or a skipped test:

```bash
LLM_PROMPT_TESTING_REPLAY_ONLY=true dotnet test
```

`LLM_PROMPT_TESTING_REPLAY_ONLY` is mutually exclusive with `LLM_PROMPT_TESTING_FORCE_API` and `LLM_PROMPT_TESTING_COMMIT_MISSING_SNAPSHOTS` (replay-only never records, so there is nothing to force or commit); enabling it alongside either throws at fixture construction.

| `LLM_PROMPT_TESTING_REPLAY_ONLY` | Cache exists? | Behavior |
|---|---|---|
| `true` | Yes | Returns cached response (no API key needed) |
| `true` | No | Throws `SnapshotNotFoundException` — no API call |

## Auto-committing recorded snapshots

Set `LLM_PROMPT_TESTING_COMMIT_MISSING_SNAPSHOTS=true` (or `1`) so that whenever a snapshot is recorded on a cache miss, it is immediately `git add`-ed and committed (scoped to just that file). This stops a freshly recorded snapshot from being accidentally left out of a commit and resurfacing as a missing-snapshot failure in CI later.

It is best-effort: if `git` is unavailable or the snapshot directory is not inside a repository, recording still succeeds and nothing is committed. Use it locally while re-recording after a prompt change:

```bash
LLM_PROMPT_TESTING_COMMIT_MISSING_SNAPSHOTS=true ANTHROPIC_API_KEY=sk-... dotnet test
```
