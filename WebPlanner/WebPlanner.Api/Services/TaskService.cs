using Microsoft.EntityFrameworkCore;
using WebPlanner.Api.Data;
using WebPlanner.Api.Models;
using WebPlanner.Shared.DTOs;
using WebPlanner.Shared.Enums;

namespace WebPlanner.Api.Services;

public class TaskService
{
    private readonly PlannerDbContext _db;

    public TaskService(PlannerDbContext db)
    {
        _db = db;
    }

    public async Task<List<PlannerTask>> GetTasksAsync(DateTime? date = null)
    {
        var query = _db.Tasks.AsNoTracking().AsQueryable();

        if (date.HasValue)
        {
            var start = date.Value.Date;
            var end = start.AddDays(1).AddTicks(-1);
            query = query.Where(t => t.DueDate >= start && t.DueDate <= end);
        }

        return await query.OrderBy(t => t.Priority).ThenBy(t => t.DueTime).ToListAsync();
    }

    public async Task<PlannerTask?> GetByIdAsync(Guid id)
    {
        return await _db.Tasks.FindAsync(id);
    }

    public async Task<PlannerTask> CreateAsync(CreateTaskRequest request)
    {
        var task = new PlannerTask
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            ParentId = request.ParentId,
            Priority = request.Priority,
            Status = PlannerTaskStatus.Todo,
            DueDate = request.DueDate,
            DueTime = request.DueTime,
            Hashtags = request.Hashtags,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Tasks.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<bool> UpdateStatusAsync(Guid id, PlannerTaskStatus status)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return false;

        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var task = await _db.Tasks.FindAsync(id);
        if (task == null) return false;

        _db.Tasks.Remove(task);
        await _db.SaveChangesAsync();
        return true;
    }
}