using LiveCounter.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace LiveCounter.Data;

public class LiveCounterDbContext(DbContextOptions<LiveCounterDbContext> options) : DbContext(options)
{
    public DbSet<LiveCounterState> Counters => Set<LiveCounterState>();
    public DbSet<LiveCounterUser> Users => Set<LiveCounterUser>();
    public DbSet<SupportTicket> Tickets => Set<SupportTicket>();
    public DbSet<SocialRoom> Rooms => Set<SocialRoom>();
    public DbSet<SocialRoomMember> RoomMembers => Set<SocialRoomMember>();
    public DbSet<RoomInvite> RoomInvites => Set<RoomInvite>();

    public void EnsureCounterExists()
    {
        if (!Counters.Any(counter => counter.Id == 1))
        {
            Counters.Add(new LiveCounterState { Id = 1 });
            SaveChanges();
        }
    }

    public void EnsureRootUser(string? email, string? password, IPasswordHasher<LiveCounterUser> hasher)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || Users.Any(user => user.IsRoot))
        {
            return;
        }

        var root = new LiveCounterUser
        {
            Email = email.Trim().ToLowerInvariant(),
            DisplayName = "Root administrator",
            IsRoot = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
        root.PasswordHash = hasher.HashPassword(root, password);
        Users.Add(root);
        SaveChanges();
    }
}
