
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using TaskService.Domain.Data;
using TaskService.Domain.Models;
using TaskService.Domain;


namespace TaskService.Services
{
    public class OverdueCheckerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OverdueCheckerService> _logger;


        public OverdueCheckerService(IServiceScopeFactory scopeFactory, ILogger<OverdueCheckerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
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
                    var now = DateTime.UtcNow;
                    var overdueTasks = await db.Tasks
                        .Where(t => t.Status != JobTaskStatus.Completed && t.Status != JobTaskStatus.Overdue)
                        .Where(t => t.DueDate < now)
                        .ToListAsync(stoppingToken);

                    foreach (var task in overdueTasks)
                    {
                        task.Status = JobTaskStatus.Overdue;
                    }

                    if (overdueTasks.Any())
                        await db.SaveChangesAsync(stoppingToken);
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
