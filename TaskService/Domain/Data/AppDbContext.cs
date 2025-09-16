using Microsoft.EntityFrameworkCore;
using TaskService.Domain.Models;

namespace TaskService.Domain.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TaskModel> Tasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TaskModel>()
            .Property(t => t.Status)
            .HasConversion<int>();

        modelBuilder.Entity<TaskModel>()
            .HasIndex(t => t.Status);

        base.OnModelCreating(modelBuilder);
    }
}
