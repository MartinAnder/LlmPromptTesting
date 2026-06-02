using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace LlmPromptTesting.Tests.CachingChatClient;

public class when_a_request_misses_the_cache_in_replay_only_mode
{
    [Fact]
    public async Task it_throws_without_calling_the_inner_client()
    {
        // Arrange
        var inner = new Mock<IChatClient>(MockBehavior.Strict);
        var snapshotDirectory = Path.Combine(
            Path.GetTempPath(),
            $"lpt-test-{Guid.NewGuid():N}");
        var sut = new LlmPromptTesting.CachingChatClient(
            innerClient: inner.Object,
            snapshotDirectory: snapshotDirectory,
            replayOnly: true);

        // Act
        var act = async () => await sut.GetResponseAsync(
            [new ChatMessage(ChatRole.User, "hello")],
            new ChatOptions { ModelId = "test-model" },
            TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<SnapshotNotFoundException>(act);
        inner.VerifyNoOtherCalls();
    }
}
