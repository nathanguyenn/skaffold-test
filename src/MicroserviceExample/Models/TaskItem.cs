namespace MicroserviceExample.Models;

public class TaskItem
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public required string Title { get; set; }
    public bool IsDone { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Project? Project { get; set; }
}
