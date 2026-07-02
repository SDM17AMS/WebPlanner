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
app.UseCors("AllowClient");

// In-memory stores
var journalStore = new Dictionary<string, string>();
var habitStore = new Dictionary<string, bool>();
var comments = new Dictionary<Guid, List<CommentDto>>();
var attachments = new Dictionary<Guid, List<AttachmentDto>>();

// Ensure uploads directory exists
var uploadsDir = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadsDir);

// Seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PlannerDbContext>();
    SeedData(db);
}

// TASK ENDPOINTS
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

app.MapPut("/api/tasks/{id:guid}", async (Guid id, CreateTaskRequest request, TaskService service) =>
{
    var success = await service.UpdateAsync(id, request);
    return success ? Results.NoContent() : Results.NotFound();
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

// JOURNAL ENDPOINTS
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

// HABIT ENDPOINTS
app.MapGet("/api/habits/{date}", (DateTime date) =>
{
    var key = date.ToString("yyyy-MM-dd");
    return Results.Ok(new { completed = habitStore.GetValueOrDefault(key, false) });
});

app.MapPost("/api/habits/{date}", (DateTime date) =>
{
    var key = date.ToString("yyyy-MM-dd");
    habitStore[key] = true;
    return Results.Ok(new { completed = true });
});

app.MapDelete("/api/habits/{date}", (DateTime date) =>
{
    var key = date.ToString("yyyy-MM-dd");
    habitStore.Remove(key);
    return Results.Ok(new { completed = false });
});

// COMMENT ENDPOINTS
app.MapGet("/api/tasks/{id:guid}/comments", (Guid id) =>
{
    var list = comments.GetValueOrDefault(id, new());
    return Results.Ok(list);
});

app.MapPost("/api/tasks/{id:guid}/comments", (Guid id, CommentRequest req) =>
{
    if (!comments.ContainsKey(id)) comments[id] = new();
    var comment = new CommentDto
    {
        Id = Guid.NewGuid(),
        TaskId = id,
        Content = req.Content,
        CreatedAt = DateTime.UtcNow
    };
    comments[id].Add(comment);
    return Results.Ok(comment);
});

// ATTACHMENT ENDPOINTS
app.MapGet("/api/tasks/{id:guid}/attachments", (Guid id) =>
{
    var list = attachments.GetValueOrDefault(id, new());
    return Results.Ok(list);
});

app.MapPost("/api/tasks/{id:guid}/attachments", async (Guid id, IFormFile file) =>
{
    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
    var filePath = Path.Combine(uploadsDir, fileName);
    await using var stream = File.Create(filePath);
    await file.CopyToAsync(stream);

    if (!attachments.ContainsKey(id)) attachments[id] = new();
    var att = new AttachmentDto
    {
        Id = Guid.NewGuid(),
        TaskId = id,
        FileName = file.FileName,
        Url = $"/uploads/{fileName}"
    };
    attachments[id].Add(att);
    return Results.Ok(att);
});

app.Run();

// SEED DATA
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

// EXTENSION METHOD
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

// REQUEST CLASSES (must be before app.Run in top-level, but C# allows after if no top-level statements follow)
public class JournalEntryRequest
{
    public string Content { get; set; } = string.Empty;
}

public class CommentRequest
{
    public string Content { get; set; } = string.Empty;
}