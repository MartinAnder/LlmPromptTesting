using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace LlmPromptTesting.Tests.CachingChatClient;

public class when_a_cached_snapshot_is_replayed_with_a_committer_configured
{
    [Fact]
    public async Task it_does_not_commit_anything()
    {
        // Arrange
        var snapshotDirectory = Path.Combine(
            Path.GetTempPath(),
            $"lpt-test-{Guid.NewGuid():N}");
        ChatMessage[] messages = [new ChatMessage(ChatRole.User, "hello")];
        var options = new ChatOptions { ModelId = "test-model" };

        var inner = new Mock<IChatClient>();
        inner
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, "answer")));

        var recorder = new LlmPromptTesting.CachingChatClient(
            inner.Object,
            snapshotDirectory);
        await recorder.GetResponseAsync(
            messages,
            options,
            TestContext.Current.CancellationToken);

        var committer = new Mock<ISnapshotCommitter>(MockBehavior.Strict);
        var sut = new LlmPromptTesting.CachingChatClient(
            innerClient: inner.Object,
            snapshotDirectory: snapshotDirectory,
            snapshotCommitter: committer.Object);

        // Act
        await sut.GetResponseAsync(
            messages,
            options,
            TestContext.Current.CancellationToken);

        // Assert
        committer.VerifyNoOtherCalls();
    }
}
