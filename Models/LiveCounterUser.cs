namespace LiveCounter.Models;

public class LiveCounterUser
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string Theme { get; set; } = "studio";
    public string Accent { get; set; } = "#ef8354";
    public string Status { get; set; } = "online";
    public string StatusMessage { get; set; } = string.Empty;
    public bool IsRoot { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}
