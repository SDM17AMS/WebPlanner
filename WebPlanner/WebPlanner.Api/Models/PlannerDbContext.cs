using Microsoft.EntityFrameworkCore;
using WebPlanner.Api.Models;

namespace WebPlanner.Api.Data;

public class PlannerDbContext : DbContext
{
    public PlannerDbContext(DbContextOptions<PlannerDbContext> options) : base(options) { }

    public DbSet<PlannerTask> Tasks => Set<PlannerTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlannerTask>()
            .HasMany(t => t.Subtasks)
            .WithOne(t => t.Parent)
            .HasForeignKey(t => t.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}