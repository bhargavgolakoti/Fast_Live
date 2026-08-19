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

public class LoginModel(
    IDbContextFactory<LiveCounterDbContext> contextFactory,
    IPasswordHasher<LiveCounterUser> passwordHasher) : PageModel
{
    [BindProperty]
    public LoginInput Input { get; set; } = new();

    public string? AccountCreatedMessage => TempData["AccountCreatedMessage"] as string;

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Input.Email = Input.Email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (!ModelState.IsValid) return Page();

        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var user = await database.Users.SingleOrDefaultAsync(item => item.Email == Input.Email, cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, Input.Password!) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Email or password is incorrect.");
            return Page();
        }

        user.LastSeenAt = DateTimeOffset.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        await SignInAsync(user);
        return Redirect("/");
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

public class LoginInput
{
    [Required, EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string? Password { get; set; }
}
