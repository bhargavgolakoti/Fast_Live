using System.Security.Claims;
using LiveCounter.Data;
using LiveCounter.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiveCounter.Api;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    IDbContextFactory<LiveCounterDbContext> contextFactory,
    IPasswordHasher<LiveCounterUser> passwordHasher) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (!IsValidEmail(email) || request.Password.Length < 12 || request.DisplayName.Trim().Length < 2)
        {
            return BadRequest(new { error = "Use a valid email, a display name, and a password of at least 12 characters." });
        }

        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (await database.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return Conflict(new { error = "An account already exists for this email." });
        }

        var user = new LiveCounterUser
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
            LastSeenAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        database.Users.Add(user);
        await database.SaveChangesAsync(cancellationToken);
        await SignIn(user);
        return Ok(ToProfile(user));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var user = await database.Users.SingleOrDefaultAsync(item => item.Email == request.Email.Trim().ToLower(), cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { error = "Email or password is incorrect." });
        }

        user.LastSeenAt = DateTimeOffset.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        await SignIn(user);
        return Ok(ToProfile(user));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpPatch("status")]
    public async Task<IActionResult> Status(StatusRequest request, CancellationToken cancellationToken)
    {
        var user = await FindCurrentUser(cancellationToken);
        if (user is null) return Unauthorized();
        if (request.Status is not ("online" or "away" or "busy" or "offline"))
        {
            return BadRequest(new { error = "Status must be online, away, busy, or offline." });
        }

        user.Status = request.Status;
        user.StatusMessage = request.Message?.Trim() ?? string.Empty;
        user.LastSeenAt = DateTimeOffset.UtcNow;
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        database.Users.Update(user);
        await database.SaveChangesAsync(cancellationToken);
        return Ok(ToProfile(user));
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat(CancellationToken cancellationToken)
    {
        var user = await FindCurrentUser(cancellationToken);
        if (user is null) return Unauthorized();
        user.LastSeenAt = DateTimeOffset.UtcNow;
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        database.Users.Update(user);
        await database.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var user = await FindCurrentUser(cancellationToken);
        return user is null ? Unauthorized() : Ok(ToProfile(user));
    }

    [HttpPatch("profile")]
    public async Task<IActionResult> Profile(ProfileRequest request, CancellationToken cancellationToken)
    {
        var user = await FindCurrentUser(cancellationToken);
        if (user is null) return Unauthorized();
        if (request.DisplayName?.Trim().Length >= 2) user.DisplayName = request.DisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(request.Theme)) user.Theme = request.Theme.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(request.Accent) && System.Text.RegularExpressions.Regex.IsMatch(request.Accent, "^#[0-9a-fA-F]{6}$")) user.Accent = request.Accent;
        user.LastSeenAt = DateTimeOffset.UtcNow;
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        database.Users.Update(user);
        await database.SaveChangesAsync(cancellationToken);
        return Ok(ToProfile(user));
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (!IsValidEmail(email)) return BadRequest(new { error = "Enter a valid email address." });
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        var user = await database.Users.SingleOrDefaultAsync(item => item.Email == email, cancellationToken);
        database.Tickets.Add(new SupportTicket
        {
            UserId = user?.Id,
            Email = email,
            Subject = "Password reset request",
            Description = "A password reset was requested for this account. Verify identity before issuing a reset.",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });
        await database.SaveChangesAsync(cancellationToken);
        return Ok(new { message = "Your request was securely sent to the support queue." });
    }

    private async Task<LiveCounterUser?> FindCurrentUser(CancellationToken cancellationToken)
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(id, out var userId)) return null;
        await using var database = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await database.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    private async Task SignIn(LiveCounterUser user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.IsRoot ? "root" : "user")
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
    }

    private static object ToProfile(LiveCounterUser user) => new { user.Id, user.Email, user.DisplayName, user.Theme, user.Accent, user.Status, user.StatusMessage, user.IsRoot, user.LastSeenAt };
    private static bool IsValidEmail(string email) => new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email);
}

public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);
public record ForgotPasswordRequest(string Email);
public record ProfileRequest(string? DisplayName, string? Theme, string? Accent);
public record StatusRequest(string Status, string? Message);
