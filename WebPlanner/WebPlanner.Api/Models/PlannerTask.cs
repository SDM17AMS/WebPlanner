using WebPlanner.Shared.Enums;

namespace WebPlanner.Api.Models;

public class PlannerTask
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public PlannerTask? Parent { get; set; }
    public List<PlannerTask> Subtasks { get; set; } = new();
    public Priority Priority { get; set; }
    public PlannerTaskStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public TimeOnly? DueTime { get; set; }
    public List<string> Hashtags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}