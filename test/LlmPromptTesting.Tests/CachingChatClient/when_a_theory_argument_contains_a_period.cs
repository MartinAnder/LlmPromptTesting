using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace LlmPromptTesting.Tests.CachingChatClient;

public class when_a_theory_argument_contains_a_period
{
    [Theory]
    [InlineData("1.5 cups")]
    public async Task it_keeps_the_period_inside_the_snapshot_filename(
        string measure
    )
    {
        // Arrange
        var snapshotDirectory = Path.Combine(
            Path.GetTempPath(),
            $"lpt-test-{Guid.NewGuid():N}"
        );
        var inner = new Mock<IChatClient>();
        inner
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(
                new ChatMessage(ChatRole.Assistant, "answer")));
        var sut = new LlmPromptTesting.CachingChatClient(
            innerClient: inner.Object,
            snapshotDirectory: snapshotDirectory
        );

        // Act
        await sut.GetResponseAsync(
            [new ChatMessage(ChatRole.User, measure)],
            new ChatOptions { ModelId = "test-model" },
            TestContext.Current.CancellationToken
        );

        // Assert
        var written = Directory
            .GetFiles(
                snapshotDirectory,
                "*.json",
                SearchOption.AllDirectories)
            .Single();
        var directory = new DirectoryInfo(
            Path.GetDirectoryName(written)!).Name;
        Assert.Equal("when_a_theory_argument_contains_a_period", directory);
        Assert.Contains("1.5 cups", Path.GetFileName(written));
    }
}
