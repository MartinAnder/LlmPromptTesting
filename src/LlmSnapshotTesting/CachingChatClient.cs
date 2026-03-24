using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Xunit;
using Xunit.Sdk;

namespace LlmSnapshotTesting;

public class CachingChatClient(
    IChatClient? innerClient,
    string snapshotDirectory
) : IChatClient
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
    };

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        var messagesList = messages.ToList();
        var instructions = options?.Instructions ?? "";
        var serializedMessages = SerializeMessages(messagesList);
        var model = options?.ModelId ?? "";
        var cacheKey = ComputeCacheKey(instructions, serializedMessages, model);

        var snapshotPath = Path.Combine(snapshotDirectory, BuildFileName(cacheKey));

        if (File.Exists(snapshotPath))
        {
            var cached = JsonSerializer.Deserialize<SnapshotEnvelope>(
                await File.ReadAllTextAsync(snapshotPath, cancellationToken)
            );
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, cached!.Response));
        }

        if (innerClient is null)
        {
            throw SkipException.ForSkip(
                $"No cached snapshot found for cache key {cacheKey}. "
                + "Either set the API key environment variable "
                + "and re-run to record snapshots, or pull the latest "
                + "snapshots from git."
            );
        }

        var response = await innerClient.GetResponseAsync(messagesList, options, cancellationToken);
        var responseText = response.Text ?? "";

        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);

        var envelope = new SnapshotEnvelope
        {
            CacheKey = cacheKey,
            Instructions = instructions,
            Messages = serializedMessages,
            Model = model,
            Response = responseText,
        };

        await File.WriteAllTextAsync(
            snapshotPath,
            JsonSerializer.Serialize(envelope, WriteOptions),
            cancellationToken
        );

        return response;
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        if (innerClient is null)
            throw new InvalidOperationException("Streaming requires an inner client.");
        return innerClient.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    public object? GetService(Type serviceType, object? key = null)
        => serviceType.IsInstanceOfType(this)
            ? this
            : innerClient?.GetService(serviceType, key);

    public void Dispose() => innerClient?.Dispose();

    private static string BuildFileName(string cacheKey)
    {
        var testName = TestContext.Current.Test?.TestDisplayName;

        if (string.IsNullOrEmpty(testName))
            return $"{cacheKey}.json";

        var parts = testName.Split('.');
        var className = parts.Length >= 2 ? parts[^2] : testName;
        var methodName = parts[^1];

        return Path.Combine(className, $"{methodName}_{cacheKey}.json");
    }

    private static string ComputeCacheKey(
        string instructions,
        string serializedMessages,
        string model
    )
    {
        var combined = $"{instructions}\n---\n{serializedMessages}\n---\n{model}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string SerializeMessages(IList<ChatMessage> messages)
    {
        var canonical = messages.Select(m => new
        {
            role = m.Role.Value,
            content = m.Text,
        });
        return JsonSerializer.Serialize(canonical);
    }

    private sealed class SnapshotEnvelope
    {
        public required string CacheKey { get; init; }
        public required string Instructions { get; init; }
        public required string Messages { get; init; }
        public required string Model { get; init; }
        public required string Response { get; init; }
    }
}
