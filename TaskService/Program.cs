using TaskService.Domain.Data;
using Microsoft.EntityFrameworkCore;
using TaskService.Services;
using Polly;
using Prometheus;
using Npgsql;
using TaskService.Messaging;
using TaskService.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

var connectionString = builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection")
?? "Host=postgres;Database=tasks_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
options.UseNpgsql(connectionString));
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("cbr", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddHostedService<OverdueCheckerService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IMqMessagePublisher, RabbitmqPublisher>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        var retry = Policy.Handle<NpgsqlException>()
                  .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(5));
        retry.Execute(db.Database.Migrate);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database migration failed on startup");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseHttpMetrics();
app.MapMetrics();

app.Run();