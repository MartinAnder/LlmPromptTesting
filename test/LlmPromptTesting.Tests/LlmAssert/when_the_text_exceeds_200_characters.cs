using Microsoft.Extensions.AI;
using Moq;
using Xunit;
using Xunit.Sdk;

namespace LlmPromptTesting.Tests.LlmAssert;

public class when_the_text_exceeds_200_characters
{
    [Fact]
    public async Task it_truncates_the_preview_to_200_characters_in_the_exception_message()
    {
        // Arrange
        var judge = new Mock<IChatClient>();
        judge
            .Setup(j => j.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatResponse(new ChatMessage(ChatRole.Assistant, "{\"result\": false}")));

        var longText = new string('a', 201);

        // Act
        var exception = await Assert.ThrowsAsync<FailException>(
            () => LlmPromptTesting.LlmAssert.JudgeAsync(judge.Object, longText, "some criterion", cancellationToken: TestContext.Current.CancellationToken));

        // Assert
        Assert.Contains(new string('a', 200) + "…", exception.Message);
        Assert.DoesNotContain(new string('a', 201), exception.Message);
    }
}


