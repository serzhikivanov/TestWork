using MonitorService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHostedService<RabbitMqConsumer>();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
