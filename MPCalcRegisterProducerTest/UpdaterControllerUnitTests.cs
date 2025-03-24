using Moq;
using Xunit;
using MPCalcRegisterProducer.Controllers;
using MPCalcRegisterProducer.Services;

public class UpdaterControllerUnitTests
{
    private readonly Mock<IRabbitMqService> _rabbitMqServiceMock;
    private readonly UpdaterController _controller;

    public UpdaterControllerUnitTests()
    {
        _rabbitMqServiceMock = new Mock<IRabbitMqService>();
        _controller = new UpdaterController(_rabbitMqServiceMock.Object);
    }

    [Fact]
    public async Task SendMessage_WhenCalled_CallsRabbitMqServiceAsync_AnyString()
    {
        // Arrange
        string message = "Test message";
        _rabbitMqServiceMock.Setup(x => x.PublishMessageAsync(It.IsAny<string>()));

        // Act
        await _controller.UpdateInQueue(message);

        // Assert
        _rabbitMqServiceMock.Verify(x => x.PublishMessageAsync(It.IsAny<string>()), Times.Once());
    }
}