using Microsoft.EntityFrameworkCore;
using WebPlanner.Api.Data;
using WebPlanner.Api.Services;
using WebPlanner.Shared.DTOs;
using WebPlanner.Shared.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PlannerDbContext>(options =>
    options.UseInMemoryDatabase("PlannerDb"));

builder.Services.AddScoped<TaskService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// In-memory journal store
var journalStore = new Dictionary<string, string>();

// Journal endpoints
app.MapGet("/api/journal/{date}", (DateTime date) =>
{
    var key = date.ToString("yyyy-MM-dd");
    return Results.Ok(new { date = key, content = journalStore.GetValueOrDefault(key, "") });
});

app.MapPut("/api/journal/{date}", (DateTime date, JournalEntryRequest request) =>
{
    var key = date.ToString("yyyy-MM-dd");
    journalStore[key] = request.Content;
    return Results.NoContent();
});

// Seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PlannerDbContext>();
    SeedData(db);
}

// Minimal API endpoints
app.MapGet("/api/tasks", async (DateTime? date, TaskService service) =>
{
    var tasks = await service.GetTasksAsync(date);
    return Results.Ok(tasks.Select(t => t.ToDto()));
});

app.MapGet("/api/tasks/{id:guid}", async (Guid id, TaskService service) =>
{
    var task = await service.GetByIdAsync(id);
    return task is null ? Results.NotFound() : Results.Ok(task.ToDto());
});

app.MapPost("/api/tasks", async (CreateTaskRequest request, TaskService service) =>
{
    var task = await service.CreateAsync(request);
    return Results.Created($"/api/tasks/{task.Id}", task.ToDto());
});

app.MapPatch("/api/tasks/{id:guid}/status", async (Guid id, PlannerTaskStatus status, TaskService service) =>
{
    var success = await service.UpdateStatusAsync(id, status);
    return success ? Results.NoContent() : Results.NotFound();
});

app.MapDelete("/api/tasks/{id:guid}", async (Guid id, TaskService service) =>
{
    var success = await service.DeleteAsync(id);
    return success ? Results.NoContent() : Results.NotFound();
});

app.UseCors("AllowClient");

app.MapPut("/api/tasks/{id:guid}", async (Guid id, CreateTaskRequest request, TaskService service) =>
{
    var success = await service.UpdateAsync(id, request);
    return success ? Results.NoContent() : Results.NotFound();
});

app.Run();

static void SeedData(PlannerDbContext db)
{
    if (db.Tasks.Any()) return;

    var today = DateTime.UtcNow.Date;

    db.Tasks.AddRange(
        new WebPlanner.Api.Models.PlannerTask
        {
            Id = Guid.NewGuid(),
            Title = "Review project proposal",
            Description = "Check requirements and budget",
            Priority = Priority.High,
            Status = PlannerTaskStatus.Todo,
            DueDate = today,
            DueTime = new TimeOnly(9, 0),
            Hashtags = new() { "work", "urgent" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new WebPlanner.Api.Models.PlannerTask
        {
            Id = Guid.NewGuid(),
            Title = "Buy groceries",
            Description = "Milk, eggs, bread",
            Priority = Priority.Medium,
            Status = PlannerTaskStatus.Todo,
            DueDate = today,
            DueTime = new TimeOnly(18, 0),
            Hashtags = new() { "personal" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        },
        new WebPlanner.Api.Models.PlannerTask
        {
            Id = Guid.NewGuid(),
            Title = "Morning workout",
            Description = "30 min cardio",
            Priority = Priority.High,
            Status = PlannerTaskStatus.Done,
            DueDate = today,
            DueTime = new TimeOnly(7, 0),
            Hashtags = new() { "health" },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        }
    );

    db.SaveChanges();
}

// Extension method to map entity to DTO
public static class TaskExtensions
{
    public static TaskDto ToDto(this WebPlanner.Api.Models.PlannerTask task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        ParentId = task.ParentId,
        Priority = task.Priority,
        Status = task.Status,
        DueDate = task.DueDate,
        DueTime = task.DueTime,
        Hashtags = task.Hashtags,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };
}

public class JournalEntryRequest
{
    public string Content { get; set; } = string.Empty;
}