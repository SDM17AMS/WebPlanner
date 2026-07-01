using WebPlanner.Shared.Enums;

namespace WebPlanner.Shared.DTOs;

public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public Priority Priority { get; set; }
    public PlannerTaskStatus Status { get; set; }
    public DateTime? DueDate { get; set; }
    public TimeOnly? DueTime { get; set; }
    public List<string> Hashtags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}