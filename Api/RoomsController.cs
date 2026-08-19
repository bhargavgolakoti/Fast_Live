using System.Security.Claims;
using LiveCounter.Data;
using LiveCounter.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiveCounter.Api;

[ApiController]
[Route("api/v1/rooms")]
[Authorize]
public class RoomsController(IDbContextFactory<LiveCounterDbContext> contextFactory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var memberships = await database.RoomMembers.AsNoTracking().Where(member => member.UserId == userId && member.ApprovalStatus == "approved").ToListAsync(cancellationToken);
        var roomIds = memberships.Select(member => member.RoomId).ToArray();
        var rooms = await database.Rooms.AsNoTracking().Where(room => room.Visibility == "public" || roomIds.Contains(room.Id)).ToListAsync(cancellationToken);
        return Ok(rooms.OrderByDescending(room => room.CreatedAt).Select(room => new { room.Id, room.Name, room.Visibility, room.ApprovalMode, room.OwnerId, memberCount = database.RoomMembers.Count(member => member.RoomId == room.Id && member.ApprovalStatus == "approved") }).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 80) return BadRequest(new { error = "Room name is required and must be 80 characters or fewer." });
        if (request.Visibility is not ("public" or "private") || request.ApprovalMode is not ("any" or "all")) return BadRequest(new { error = "Choose public/private visibility and any/all approval mode." });
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var room = new SocialRoom { Name = request.Name.Trim(), Visibility = request.Visibility, ApprovalMode = request.ApprovalMode, OwnerId = userId, CreatedAt = DateTimeOffset.UtcNow };
        database.Rooms.Add(room);
        await database.SaveChangesAsync(cancellationToken);
        database.RoomMembers.Add(new SocialRoomMember { RoomId = room.Id, UserId = userId, Role = "owner", ApprovalStatus = "approved", JoinedAt = DateTimeOffset.UtcNow, RespondedAt = DateTimeOffset.UtcNow });
        await database.SaveChangesAsync(cancellationToken);
        return Ok(ToRoom(room, 1));
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var invites = await database.RoomInvites.AsNoTracking().Where(invite => invite.InvitedUserId == userId && invite.Status == "pending").Join(database.Rooms, invite => invite.RoomId, room => room.Id, (invite, room) => new { invite.Id, invite.RoomId, invite.CreatedAt, room.Name, room.Visibility, room.ApprovalMode }).ToListAsync(cancellationToken);
        return Ok(invites.OrderByDescending(invite => invite.CreatedAt));
    }

    [HttpPost("{roomId:int}/invites")]
    public async Task<IActionResult> Invite(int roomId, InviteRequest request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var room = await database.Rooms.SingleOrDefaultAsync(item => item.Id == roomId, cancellationToken);
        if (room is null) return NotFound();
        if (!await IsOwner(database, roomId, userId, cancellationToken)) return Forbid();
        var emails = request.Emails.Where(email => !string.IsNullOrWhiteSpace(email)).Select(email => email.Trim().ToLowerInvariant()).Distinct().Take(20).ToArray();
        var ownerEmails = (request.OwnerEmails ?? Array.Empty<string>()).Where(email => !string.IsNullOrWhiteSpace(email)).Select(email => email.Trim().ToLowerInvariant()).ToHashSet();
        var users = await database.Users.Where(user => emails.Contains(user.Email) && user.Id != userId).ToListAsync(cancellationToken);
        var existing = await database.RoomInvites.Where(invite => invite.RoomId == roomId && users.Select(user => user.Id).Contains(invite.InvitedUserId) && invite.Status == "pending").Select(invite => invite.InvitedUserId).ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var user in users.Where(user => !existing.Contains(user.Id)))
        {
            database.RoomInvites.Add(new RoomInvite { RoomId = roomId, InvitedUserId = user.Id, InvitedByUserId = userId, CreatedAt = now });
            database.RoomMembers.Add(new SocialRoomMember { RoomId = roomId, UserId = user.Id, Role = ownerEmails.Contains(user.Email) ? "owner" : "member", ApprovalStatus = "pending", JoinedAt = now });
        }
        await database.SaveChangesAsync(cancellationToken);
        return Ok(new { invited = users.Count(user => !existing.Contains(user.Id)) });
    }

    [HttpPost("{roomId:int}/join")]
    public async Task<IActionResult> Join(int roomId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var room = await database.Rooms.SingleOrDefaultAsync(item => item.Id == roomId && item.Visibility == "public", cancellationToken);
        if (room is null) return NotFound();
        if (await database.RoomMembers.AnyAsync(member => member.RoomId == roomId && member.UserId == userId && member.ApprovalStatus != "declined", cancellationToken)) return Conflict(new { error = "You already have a membership request for this room." });
        var now = DateTimeOffset.UtcNow;
        database.RoomMembers.Add(new SocialRoomMember { RoomId = roomId, UserId = userId, ApprovalStatus = "pending", JoinedAt = now });
        await database.SaveChangesAsync(cancellationToken);
        return Ok(new { status = "pending", message = "Your request is waiting for the room approval rule." });
    }

    [HttpPost("invites/{inviteId:int}/respond")]
    public async Task<IActionResult> Respond(int inviteId, InviteResponse request, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var invite = await database.RoomInvites.SingleOrDefaultAsync(item => item.Id == inviteId && item.InvitedUserId == userId && item.Status == "pending", cancellationToken);
        if (invite is null) return NotFound();
        if (request.Decision is not ("approve" or "decline")) return BadRequest(new { error = "Decision must be approve or decline." });
        var member = await database.RoomMembers.SingleAsync(item => item.RoomId == invite.RoomId && item.UserId == userId, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        invite.Status = request.Decision == "approve" ? "approved" : "declined";
        invite.RespondedAt = now;
        member.ApprovalStatus = invite.Status;
        member.RespondedAt = now;
        await database.SaveChangesAsync(cancellationToken);
        return Ok(new { invite.Status, roomId = invite.RoomId, roomOpen = await IsRoomOpen(database, invite.RoomId, cancellationToken) });
    }

    [HttpGet("{roomId:int}")]
    public async Task<IActionResult> Details(int roomId, CancellationToken cancellationToken)
    {
        var userId = CurrentUserId();
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var room = await database.Rooms.AsNoTracking().SingleOrDefaultAsync(item => item.Id == roomId, cancellationToken);
        if (room is null) return NotFound();
        var member = await database.RoomMembers.AsNoTracking().SingleOrDefaultAsync(item => item.RoomId == roomId && item.UserId == userId, cancellationToken);
        if (room.Visibility == "private" && member?.ApprovalStatus != "approved") return Forbid();
        var members = await database.RoomMembers.AsNoTracking().Where(item => item.RoomId == roomId && item.ApprovalStatus == "approved").Join(database.Users, member => member.UserId, user => user.Id, (member, user) => new { user.Id, user.DisplayName, user.Status, user.StatusMessage, user.LastSeenAt, member.Role }).ToListAsync(cancellationToken);
        return Ok(new { room.Id, room.Name, room.Visibility, room.ApprovalMode, room.OwnerId, isMember = member?.ApprovalStatus == "approved", isOwner = member?.Role == "owner" && member.ApprovalStatus == "approved", roomOpen = await IsRoomOpen(database, roomId, cancellationToken), members });
    }

    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static async Task<bool> IsOwner(LiveCounterDbContext database, int roomId, int userId, CancellationToken cancellationToken) => await database.RoomMembers.AnyAsync(member => member.RoomId == roomId && member.UserId == userId && member.Role == "owner" && member.ApprovalStatus == "approved", cancellationToken);
    private static object ToRoom(SocialRoom room, int members) => new { room.Id, room.Name, room.Visibility, room.ApprovalMode, room.OwnerId, memberCount = members };
    private static async Task<bool> IsRoomOpen(LiveCounterDbContext database, int roomId, CancellationToken cancellationToken)
    {
        var room = await database.Rooms.AsNoTracking().SingleAsync(item => item.Id == roomId, cancellationToken);
        var members = await database.RoomMembers.AsNoTracking().Where(member => member.RoomId == roomId && member.Role != "owner").ToListAsync(cancellationToken);
        return members.Count == 0 || (room.ApprovalMode == "any" ? members.Any(member => member.ApprovalStatus == "approved") : members.All(member => member.ApprovalStatus == "approved"));
    }
}

public record CreateRoomRequest(string Name, string Visibility, string ApprovalMode);
public record InviteRequest(string[] Emails, string[]? OwnerEmails);
public record InviteResponse(string Decision);
