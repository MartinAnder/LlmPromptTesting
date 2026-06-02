using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace LlmPromptTesting.Tests.CachingChatClient;

public class when_a_request_hits_the_cache_in_replay_only_mode
{
    [Fact]
    public async Task it_replays_the_cached_response()
    {
        // Arrange
        var snapshotDirectory = Path.Combine(
            Path.GetTempPath(),
            $"lpt-test-{Guid.NewGuid():N}");
        ChatMessage[] messages = [new ChatMessage(ChatRole.User, "hello")];
        var options = new ChatOptions { ModelId = "test-model" };

        var recordingInner = new Mock<IChatClient>();
        recordingInner
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, "recorded answer")));

        var recorder = new LlmPromptTesting.CachingChatClient(
            recordingInner.Object,
            snapshotDirectory);
        await recorder.GetResponseAsync(
            messages,
            options,
            TestContext.Current.CancellationToken);

        var replayInner = new Mock<IChatClient>(MockBehavior.Strict);
        var sut = new LlmPromptTesting.CachingChatClient(
            innerClient: replayInner.Object,
            snapshotDirectory: snapshotDirectory,
            replayOnly: true);

        // Act
        var response = await sut.GetResponseAsync(
            messages,
            options,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("recorded answer", response.Text);
        replayInner.VerifyNoOtherCalls();
    }
}
