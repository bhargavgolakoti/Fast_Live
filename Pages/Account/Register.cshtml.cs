using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using LiveCounter.Data;
using LiveCounter.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LiveCounter.Pages.Account;

public class RegisterModel(
    IDbContextFactory<LiveCounterDbContext> contextFactory,
    IPasswordHasher<LiveCounterUser> passwordHasher) : PageModel
{
    [BindProperty]
    public RegisterInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Input.Email = Input.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        Input.DisplayName = Input.DisplayName?.Trim() ?? string.Empty;
        if (!ModelState.IsValid) return Page();

        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await database.Users.AnyAsync(user => user.Email == Input.Email, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "An account already exists for this email.");
            return Page();
        }

        var user = new LiveCounterUser
        {
            Email = Input.Email,
            DisplayName = Input.DisplayName,
            PasswordHash = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, Input.Password!);
        database.Users.Add(user);
        await database.SaveChangesAsync(cancellationToken);
        await SignInAsync(user);
        return Redirect("/?created=1");
    }

    private Task SignInAsync(LiveCounterUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.IsRoot ? "root" : "user")
        };
        return HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
    }
}

public class RegisterInput
{
    [Required, StringLength(80, MinimumLength = 2)]
    public string? DisplayName { get; set; }

    [Required, EmailAddress]
    public string? Email { get; set; }

    [Required, StringLength(128, MinimumLength = 12)]
    public string? Password { get; set; }
}
