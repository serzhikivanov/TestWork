using Microsoft.EntityFrameworkCore;
using TaskService.Domain.Data;
using TaskService.Domain;
using TaskService.Interfaces;
using System.Threading.Tasks;


namespace TaskService.Services
{
    public class OverdueCheckerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OverdueCheckerService> _logger;
        private readonly IMqMessagePublisher _mqMessagePublisher;

        public OverdueCheckerService(IServiceScopeFactory scopeFactory, ILogger<OverdueCheckerService> logger, IMqMessagePublisher mqMessagePublisher)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _mqMessagePublisher = mqMessagePublisher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Ensure the database and table exist before running
            await db.Database.EnsureCreatedAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_mqMessagePublisher.ConnectionReady)
                    {
                        var now = DateTime.UtcNow;
                        var overdueTasks = await db.Tasks
                            .Where(t => t.Status != JobTaskStatus.Completed && t.Status != JobTaskStatus.Overdue)
                            .Where(t => t.DueDate < now)
                            .ToListAsync(stoppingToken);

                        foreach (var task in overdueTasks)
                        {
                            task.Status = JobTaskStatus.Overdue;
                            _logger.LogInformation($"Setting {task.Id} to overdue");
                            _mqMessagePublisher.Publish("tasks.events", new { TaskId = task.Id, Action = "Overdue" });
                        }

                        if (overdueTasks.Any())
                            await db.SaveChangesAsync(stoppingToken);
                    }
                    else
                        _logger.LogWarning($"Waiting for RabbitMQ connection...");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OverdueCheckerService");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
