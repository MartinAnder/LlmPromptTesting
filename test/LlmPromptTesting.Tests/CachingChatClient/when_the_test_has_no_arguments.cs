using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace LlmPromptTesting.Tests.CachingChatClient;

public class when_the_test_has_no_arguments
{
    [Fact]
    public async Task it_names_the_snapshot_after_the_method_with_no_parentheses()
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
            [new ChatMessage(ChatRole.User, "hi")],
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
        Assert.Equal("when_the_test_has_no_arguments", directory);
        var fileName = Path.GetFileName(written);
        Assert.StartsWith(
            "it_names_the_snapshot_after_the_method_with_no_parentheses_",
            fileName
        );
        Assert.DoesNotContain("(", fileName);
    }
}
