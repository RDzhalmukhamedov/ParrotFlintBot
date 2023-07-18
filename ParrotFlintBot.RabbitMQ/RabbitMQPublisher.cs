using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Shared;
using RabbitMQ.Client;

namespace ParrotFlintBot.RabbitMQ;

public class RabbitMQPublisher
{
    private readonly ILogger _logger;
    private readonly IModel? _channel;
    private readonly RabbitMQConfiguration _config;

    public RabbitMQPublisher(
        IOptions<RabbitMQConfiguration> rabbitConfig,
        IOptions<AppConfig> appConfig,
        ILogger<RabbitMQPublisher> logger)
    {
        _logger = logger;
        _config = rabbitConfig.Value;
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _config.Host,
                Port = _config.Port,
                UserName = _config.Username,
                Password = _config.Password,
                VirtualHost = _config.Username,
                DispatchConsumersAsync = true,
                ClientProvidedName = $"{ appConfig.Value.Name } Publisher"
            };
            var connection = factory.CreateConnection();
            _channel = connection.CreateModel();
            if (rabbitConfig.Value.EnableAckConfirmation)
            {
                _channel.ConfirmSelect();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("RabbitMQPublisher init error with exception: {Exception}", ex);
        }
    }

    public void PushMessage(string routeKey, object message, int? ttl = null)
    {
        if (_channel is null)
        {
            _logger.LogError("Channel is not initialized, pushing message to routeKey: {RouteKey}, will be stopped",
                routeKey);
            return;
        }
        _logger.LogInformation("PushMessage, routeKey: {RouteKey}", routeKey);
        try
        {
            _channel.QueueDeclare(queue: routeKey,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            _channel.QueueBind(queue: routeKey,
                exchange: "message",
                routingKey: routeKey);
            string serializedMessage = JsonSerializer.Serialize(message);
            IBasicProperties? props = null;
            if (ttl is not null)
            {
                props = _channel.CreateBasicProperties();
                props.Expiration = (ttl * 1000).ToString();
            }

            var body = Encoding.UTF8.GetBytes(serializedMessage);
            _channel.BasicPublish(exchange: "message",
                routingKey: routeKey,
                basicProperties: props,
                body: body);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "RabbitMQPublisher pushing message with routeKey: {RouteKey}, failed with exception: {Exception}",
                routeKey, ex);
        }
    }

    public bool WaitForAck()
    {
        return !_config.EnableAckConfirmation ||
               _channel?.WaitForConfirms(TimeSpan.FromSeconds(_config.MessageTTL)) is true;
    }
}