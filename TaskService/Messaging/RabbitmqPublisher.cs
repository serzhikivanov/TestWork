using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using TaskService.Interfaces;

namespace TaskService.Messaging
{
    public class RabbitmqPublisher : IMqMessagePublisher, IDisposable
    {
        private readonly ILogger<IMqMessagePublisher> _logger;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _connectionTask;

        private IConnection? _connection;
        private IModel? _channel;

        public bool ConnectionReady => _channel != null;

        public RabbitmqPublisher(ILogger<IMqMessagePublisher> logger)
        {
            _logger = logger;

            _connectionTask = Task.Run(() => ConnecAsync(_cts.Token), _cts.Token);
        }

        public bool Publish<T>(string queueName, T message)
        {
            if (!ConnectionReady)
                return false;

            var json = JsonSerializer.Serialize(message);
            var payload = (queueName, json);
            return TryPublish(payload);
        }

        private async Task ConnecAsync(CancellationToken cancellationToken)
        {
            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                Port = 5672,
                UserName = "guest",
                Password = "guest"
            };

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    _logger.LogInformation("RabbitMQ Publisher connected.");
                    break; // exit loop after successful connection
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to connect to RabbitMQ, retrying - {0}", ex.Message);
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        private bool TryPublish((string Queue, string Payload) item)
        {
            try
            {
                _channel?.QueueDeclare(queue: item.Queue, durable: true, exclusive: false, autoDelete: false, arguments: null);

                var body = Encoding.UTF8.GetBytes(item.Payload);

                _channel?.BasicPublish(exchange: "", routingKey: item.Queue, basicProperties: null, body: body);

                _logger.LogInformation("Published message to {Queue}: {Message}", item.Queue, item.Payload);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message. Re-queuing.");
                return false;
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try { _connectionTask.Wait(); } catch { /* ignore */ }
            _channel?.Close();
            _connection?.Close();
            _cts.Dispose();
        }
    }
}