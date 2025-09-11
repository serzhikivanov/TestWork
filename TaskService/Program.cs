using TaskService.Domain.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using TaskService.Services;
using Polly;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Logging
builder.Logging.AddConsole();

// Configuration for PostgreSQL connection
var connectionString = builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection")
?? "Host=postgres;Database=tasks_db;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(options =>
options.UseNpgsql(connectionString));

builder.Services.AddControllers();
// Memory cache for currency caching
builder.Services.AddMemoryCache();
// HttpClient for external API
builder.Services.AddHttpClient("cbr", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});
// Add background service
builder.Services.AddHostedService<OverdueCheckerService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Apply pending migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        var retry = Policy.Handle<NpgsqlException>()
                  .WaitAndRetry(5, retryAttempt => TimeSpan.FromSeconds(5));

        /*
         OPTIONAL retry to avoid warning on initial database connection 
         The "relation "__EFMigrationsHistory" does not exist" message only happens the very first time on a new database

        var retry = Policy
            .Handle<Npgsql.NpgsqlException>()
            .Or<TimeoutException>()
            .WaitAndRetry(
                retryCount: 5,
                sleepDurationProvider: _ => TimeSpan.FromSeconds(5),
                onRetry: (ex, ts) =>
                {
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(ex, "Retrying database migration...");
                });
         */

        retry.Execute(() => db.Database.Migrate());
        //db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Database migration failed on startup");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();