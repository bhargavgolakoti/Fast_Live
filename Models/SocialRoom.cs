namespace LiveCounter.Models;

public class SocialRoom
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Visibility { get; set; } = "private";
    public string ApprovalMode { get; set; } = "all";
    public int OwnerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class SocialRoomMember
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "member";
    public string ApprovalStatus { get; set; } = "pending";
    public DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
}

public class RoomInvite
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int InvitedUserId { get; set; }
    public int InvitedByUserId { get; set; }
    public string Status { get; set; } = "pending";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
}
