using System.ComponentModel.DataAnnotations;
using LiveCounter.Data;
using LiveCounter.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LiveCounter.Pages.Account;

public class ForgotPasswordModel(IDbContextFactory<LiveCounterDbContext> contextFactory) : PageModel
{
    [BindProperty]
    public ForgotPasswordInput Input { get; set; } = new();

    public bool Submitted { get; private set; }
    public string Message { get; private set; } = "Your request was securely sent to the support queue.";

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Input.Email = Input.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!ModelState.IsValid) return Page();

        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var user = await database.Users.SingleOrDefaultAsync(item => item.Email == Input.Email, cancellationToken);
        database.Tickets.Add(new SupportTicket
        {
            UserId = user?.Id,
            Email = Input.Email,
            Subject = "Password reset request",
            Description = "A password reset was requested for this account. Verify identity before issuing a reset.",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await database.SaveChangesAsync(cancellationToken);
        Submitted = true;
        return Page();
    }
}

public class ForgotPasswordInput
{
    [Required, EmailAddress]
    public string? Email { get; set; }
}
