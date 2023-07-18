using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace ParrotFlintBot.RabbitMQ;

public class RabbitMQListener : IHostedService, IDisposable
{
    private bool _isDisposed;
    private readonly string? _queueName;
    private readonly ConnectionFactory? _factory;
    protected readonly ILogger<RabbitMQListener> Logger;
    protected readonly string RouteKey = string.Empty;

    private IConnection? _connection;
    private IModel? _channel;
    private CancellationTokenSource? _stoppingTokenSource;

    protected RabbitMQListener(
        IOptions<RabbitMQConfiguration> config,
        ILogger<RabbitMQListener> logger,
        string routeKeyName = "",
        string connectionName = "")
    {
        Logger = logger;
        try
        {
            _factory = new ConnectionFactory()
            {
                HostName = config.Value.Host,
                Port = config.Value.Port,
                UserName = config.Value.Username,
                Password = config.Value.Password,
                VirtualHost = config.Value.Username,
                DispatchConsumersAsync = true
            };

            config.Value.ListenerRouteKeys.TryGetValue(routeKeyName, out var route);
            RouteKey = string.IsNullOrWhiteSpace(route) ? routeKeyName : route;
            _queueName = RouteKey;
            _factory.ClientProvidedName = !string.IsNullOrWhiteSpace(connectionName)
                ? connectionName
                : $"{RouteKey} Listener";
        }
        catch (Exception ex)
        {
            Logger.LogError("RabbitMQListener for route key: {Route}, init error with exception: {Exception}",
                RouteKey, ex);
        }
    }

    public Task StartAsync(CancellationToken stoppingToken)
    {
        Register();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    protected virtual Task<bool> ProcessMessage(string message, CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
    
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        DeRegister();
        _channel?.Dispose();
        _connection?.Dispose();
    }

    private void Register()
    {
        if (_factory is null)
        {
            Logger.LogError("Channel is not initialized, pushing message to routeKey: {RouteKey}, will be stopped",
                RouteKey);
            DeRegister();
            return;
        }
        Logger.LogInformation("RabbitMQListener register, routeKey: {RouteKey}", RouteKey);
        try
        {
            _stoppingTokenSource = new CancellationTokenSource();
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(exchange: "message", type: ExchangeType.Direct, durable: true);

            _channel.QueueDeclare(queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
            _channel.QueueBind(queue: _queueName,
                exchange: "message",
                routingKey: RouteKey);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnConsumerOnReceived;

            _channel.BasicConsume(queue: _queueName, consumer: consumer);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                "RabbitMQListener registering with routeKey: {RouteKey}, failed with exception: {Exception}", RouteKey,
                ex);
            DeRegister();
        }
    }

    private void DeRegister()
    {
        Logger.LogInformation("RabbitMQListener deregister, routeKey: {RouteKey}", RouteKey);
        _connection?.Close();
        _stoppingTokenSource?.Cancel();
        _stoppingTokenSource?.Dispose();
    }

    private async Task OnConsumerOnReceived(object? model, BasicDeliverEventArgs ea)
    {
        if (_stoppingTokenSource is { Token.IsCancellationRequested: false })
        {
            var message = Encoding.UTF8.GetString(ea.Body.Span);
            var result = await ProcessMessage(message, _stoppingTokenSource.Token);
            if (result)
            {
                _channel?.BasicAck(ea.DeliveryTag, false);
            }
            else
            {
                _channel?.BasicNack(ea.DeliveryTag, false, false);
            }
        }
    }
}