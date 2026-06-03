using System.Globalization;
using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace LlmPromptTesting.Tests.CachingChatClient;

public class when_a_theory_argument_is_a_double
{
    // The InlineData value lands in the snapshot filename via the test
    // display name. Recording under a comma-decimal culture proves the
    // filename is rendered invariantly (period), not per the current locale.
    [Theory]
    [InlineData(0.45)]
    public async Task it_writes_the_argument_with_a_period_regardless_of_culture(
        double threshold
    )
    {
        // Arrange
        var originalCulture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("da-DK");
        try
        {
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
            var fileName = Path.GetFileName(written);
            var invariant = threshold.ToString(
                "G17",
                CultureInfo.InvariantCulture);
            Assert.Contains(invariant, fileName);
            Assert.DoesNotContain(",", fileName);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
