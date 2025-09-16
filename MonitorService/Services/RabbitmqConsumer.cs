using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace MonitorService.Services
{
    public class RabbitMqConsumer : BackgroundService
    {
        private readonly ILogger<RabbitMqConsumer> _logger;
        private readonly ConcurrentQueue<(string Queue, string Payload)> _messageQueue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _connectionTask;

        private IConnection? _connection;
        private IModel? _channel;

        public RabbitMqConsumer(ILogger<RabbitMqConsumer> logger)
        {
            _logger = logger;
            _connectionTask = Task.Run(() => ConnectAndStartConsumingAsync(_cts.Token), _cts.Token);
        }

        private async Task ConnectAndStartConsumingAsync(CancellationToken cancellationToken)
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
                    _channel.QueueDeclare(queue: "tasks.events",
                                          durable: true,
                                          exclusive: false,
                                          autoDelete: false,
                                          arguments: null);

                    var consumer = new EventingBasicConsumer(_channel);
                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var json = Encoding.UTF8.GetString(body);

                        _logger.LogInformation("Consumed message: {Message}", json);
                        _messageQueue.Enqueue(("tasks.events", json));
                    };

                    _channel.BasicConsume(queue: "tasks.events", autoAck: true, consumer: consumer);
                    _logger.LogInformation("RabbitMQ Consumer connected and started.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to connect to RabbitMQ, retrying - {0}", ex.Message);
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

        public override void Dispose()
        {
            _cts.Cancel();
            try 
            { 
                _connectionTask.Wait(); 
            } 
            catch { /* ignore */ }

            _channel?.Close();
            _connection?.Close();
            _cts.Dispose();
            base.Dispose();
        }
    }
}
