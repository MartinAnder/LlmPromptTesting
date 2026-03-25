using System.Text.Json;
using Microsoft.Extensions.AI;
using Xunit;

namespace LlmPromptTesting;

public static class LlmAssert
{
    public static string? DefaultModelId { get; set; }

    private const string JudgeSystemPrompt =
        "You are a binary evaluator. "
        + "You will be given a piece of text and a criterion. "
        + "Your response must be a raw JSON object and nothing else. "
        + "Do not use markdown, code blocks, headers, bullet points, or any prose. "
        + "Do not write anything before or after the JSON. "
        + "The JSON must contain exactly one boolean field named \"result\". "
        + "Output {\"result\": true} if the criterion is met, or {\"result\": false} if it is not.";

    public static Task JudgeAsync(
        IChatClient judge,
        ChatResponse response,
        string criterion,
        string? modelId = null,
        CancellationToken cancellationToken = default
    )
        => JudgeAsync(judge, response.Text ?? "", criterion, modelId, cancellationToken);

    public static async Task JudgeAsync(
        IChatClient judge,
        string text,
        string criterion,
        string? modelId = null,
        CancellationToken cancellationToken = default
    )
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.User,
                $"Text:\n{text}\n\n"
                + $"Criterion: {criterion}\n\n"
                + "Reply with only {\"result\": true} or {\"result\": false}. No other text."),
        };

        var options = new ChatOptions
        {
            Instructions = JudgeSystemPrompt,
            ModelId = modelId ?? DefaultModelId,
            MaxOutputTokens = 20,
            ResponseFormat = ChatResponseFormat.Json,
        };

        var judgeResponse = await judge.GetResponseAsync(messages, options, cancellationToken);
        var verdictText = judgeResponse.Text?.Trim() ?? "";

        try
        {
            using var doc = JsonDocument.Parse(verdictText);
            var result = doc.RootElement.GetProperty("result").GetBoolean();
            if (result)
                return;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            Assert.Fail(
                $"LLM judge returned an unexpected response format.{Environment.NewLine}"
                + $"Expected : {{\"result\": true}} or {{\"result\": false}}{Environment.NewLine}"
                + $"Received : {verdictText}"
            );
            return;
        }

        var preview = text.Length > 200
            ? text[..200] + "…"
            : text;

        Assert.Fail(
            $"LLM judge did not satisfy the criterion.{Environment.NewLine}"
            + $"Criterion : {criterion}{Environment.NewLine}"
            + $"Text      : {preview}"
        );
    }
}
