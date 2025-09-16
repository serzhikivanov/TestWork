namespace TaskService.Interfaces;

public interface IMqMessagePublisher
{
    bool ConnectionReady { get; }

    bool Publish<T>(string queueName, T message);
}
