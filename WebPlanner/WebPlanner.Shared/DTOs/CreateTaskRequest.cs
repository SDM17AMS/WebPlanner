using WebPlanner.Shared.Enums;

namespace WebPlanner.Shared.DTOs;

public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentId { get; set; }
    public Priority Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public TimeOnly? DueTime { get; set; }
    public List<string> Hashtags { get; set; } = new();
}