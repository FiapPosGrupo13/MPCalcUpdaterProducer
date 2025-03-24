﻿using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using MPCalcRegisterProducer.Controllers;
using MPCalcRegisterProducer.Services;

public class UpdaterControllerIntegrationTests
{
    private readonly IRabbitMqService _rabbitMqService;
    private readonly UpdaterController _controller;
    private readonly string _queueName = "mpcalc-updater-queue";

    public UpdaterControllerIntegrationTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "RabbitMQ:QueueName", _queueName },
                { "RabbitMQ:HostName", "localhost" },
                { "RabbitMQ:Port", "5672" },
                { "RabbitMQ:User", "guest" },
                { "RabbitMQ:Password", "guest" }
            })
            .Build();

        _rabbitMqService = new RabbitMqService(configuration);
        _controller = new UpdaterController(_rabbitMqService);
    }

    [Fact]
    public async Task SendMessage_WhenCalled_MessageIsSentToQueueAsync()
    {
        // Arrange
        string message = "Integration test message " + Guid.NewGuid().ToString();
        string expectedMessage = $"\"{message}\"";

        var factory = new ConnectionFactory
        {
            HostName = "localhost",
            Port = 5672,
            UserName = "guest",
            Password = "guest",
            RequestedConnectionTimeout = TimeSpan.FromMilliseconds(500)
        };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(_queueName, durable: true, exclusive: false, autoDelete: false, arguments: new Dictionary<string, object>
        {
            { "x-dead-letter-exchange", "mpcalc-dlx" },
            { "x-dead-letter-routing-key", "mpcalc-updater-queue-dlq" }
        });

        await channel.QueuePurgeAsync(_queueName);

        // Act
        await _controller.UpdateInQueue(message);

        // Aguarda para garantir que a mensagem seja publicada
        await Task.Delay(500);

        // Verifica o número de mensagens na fila
        var queueInfo = await channel.QueueDeclarePassiveAsync(_queueName);
        uint messageCount = queueInfo?.MessageCount ?? 0;
        Assert.True(messageCount > 0, $"Nenhuma mensagem encontrada na fila {_queueName}. Contagem: {messageCount}. Esperava pelo menos 1.");

        // Recupera a mensagem
        BasicGetResult result = await channel.BasicGetAsync(_queueName, autoAck: true);
        string receivedMessage = result != null ? Encoding.UTF8.GetString(result.Body.ToArray()) : null;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(receivedMessage);
        Assert.Equal(expectedMessage, receivedMessage);
    }
}