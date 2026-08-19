namespace LiveCounter.Models;

public class SupportTicket
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string? Email { get; set; }
    public required string Subject { get; set; }
    public required string Description { get; set; }
    public string Status { get; set; } = "open";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
