using LiveCounter.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiveCounter.Api;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "root")]
public class AdminController(IDbContextFactory<LiveCounterDbContext> contextFactory) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-2);
        var users = await database.Users.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(new
        {
            totalUsers = users.Count,
            onlineUsers = users.Count(user => user.LastSeenAt >= cutoff),
            openTickets = await database.Tickets.CountAsync(ticket => ticket.Status != "closed", cancellationToken),
            totalVisits = await database.Counters.Where(counter => counter.Id == 1).Select(counter => counter.TotalVisits).SingleAsync(cancellationToken)
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var cutoff = DateTimeOffset.UtcNow.AddMinutes(-2);
        var users = await database.Users.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(users.Select(user => new
        {
            user.Id, user.Email, user.DisplayName, user.IsRoot, online = user.LastSeenAt >= cutoff, user.LastSeenAt, user.CreatedAt
        }).OrderByDescending(user => user.LastSeenAt).ToList());
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> Tickets(CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var tickets = await database.Tickets.AsNoTracking().ToListAsync(cancellationToken);
        return Ok(tickets.OrderByDescending(ticket => ticket.UpdatedAt));
    }

    [HttpPatch("tickets/{id:int}")]
    public async Task<IActionResult> UpdateTicket(int id, TicketStatusRequest request, CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var ticket = await database.Tickets.FindAsync([id], cancellationToken);
        if (ticket is null) return NotFound();
        if (request.Status is not ("open" or "in-progress" or "closed")) return BadRequest(new { error = "Unsupported ticket status." });
        ticket.Status = request.Status;
        ticket.UpdatedAt = DateTimeOffset.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        return Ok(ticket);
    }
}

public record TicketStatusRequest(string Status);
