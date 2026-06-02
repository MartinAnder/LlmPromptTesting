using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace LlmPromptTesting.Tests.CachingChatClient;

public class when_a_snapshot_is_recorded_with_a_committer_configured
{
    [Fact]
    public async Task it_commits_the_recorded_snapshot()
    {
        // Arrange
        var snapshotDirectory = Path.Combine(
            Path.GetTempPath(),
            $"lpt-test-{Guid.NewGuid():N}");

        var inner = new Mock<IChatClient>();
        inner
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, "answer")));

        var committer = new Mock<ISnapshotCommitter>();

        var sut = new LlmPromptTesting.CachingChatClient(
            innerClient: inner.Object,
            snapshotDirectory: snapshotDirectory,
            snapshotCommitter: committer.Object);

        // Act
        await sut.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "hi")],
            new ChatOptions { ModelId = "test-model" },
            TestContext.Current.CancellationToken);

        // Assert
        committer.Verify(
            c => c.CommitAsync(
                It.Is<string>(path => path.StartsWith(snapshotDirectory)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
