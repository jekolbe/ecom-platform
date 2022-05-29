using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using NotificationWorker.Models;

namespace NotificationWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private ConnectionFactory _connectionFactory;
    private IConnection _connection;
    private IModel _channel;
    private const string QueueName = "notification_users_q";

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        var rabbitHostName = Environment.GetEnvironmentVariable("RABBIT_HOSTNAME");
        _connectionFactory = new ConnectionFactory
        {
            HostName = rabbitHostName ?? "localhost",
            Port = 5672,
            UserName = "consumer",
            Password = "consumer",
            DispatchConsumersAsync = true
        };
        _connection = _connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclarePassive(QueueName);
        _channel.BasicQos(0, 1, false);
        _logger.LogInformation($"Queue [{QueueName}] is waiting for messages.");

        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var messageCount = _channel.MessageCount(QueueName);
        if (messageCount > 0)
        {
            _logger.LogInformation($"\tDetected {messageCount} message(s).");
        }

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (bc, ea) =>
        {
            //_logger.LogInformation($"\tMessage User Id: [{ea.BasicProperties.UserId}].");
            if (ea.BasicProperties.UserId != "producer")
            {
                _logger.LogInformation($"\tIgnored a message sent by [{ea.BasicProperties.UserId}].");
                return;
            }

            var t = DateTimeOffset.FromUnixTimeMilliseconds(ea.BasicProperties.Timestamp.UnixTime);
            _logger.LogInformation($"{t.LocalDateTime:O} ID=[{ea.BasicProperties.MessageId}]");
            var message = Encoding.UTF8.GetString(ea.Body.ToArray());
            _logger.LogInformation($"Processing msg: '{message}'.");

            try
            {
                var user = JsonSerializer.Deserialize<Confirmation>(message);
                // TODO: Add user email to model
                _logger.LogInformation($"Sending confirmation for user {user.FirstName} {user.LastName} confirmation email to [].");

                await Task.Delay(new Random().Next(1, 3) * 1000, stoppingToken); // simulate an async email process
                // TODO: Send email via SMTP server

                _logger.LogInformation($"Confirmation email for user {user.FirstName} {user.LastName} sent.");
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (JsonException)
            {
                _logger.LogError($"JSON Parse Error: '{message}'.");
                _channel.BasicNack(ea.DeliveryTag, false, false);
            }
            catch (AlreadyClosedException)
            {
                _logger.LogInformation("RabbitMQ is closed!");
            }
            catch (Exception e)
            {
                _logger.LogError(default, e, e.Message);
            }
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        await Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        _connection.Close();
        _logger.LogInformation("RabbitMQ connection is closed.");
    }
}
