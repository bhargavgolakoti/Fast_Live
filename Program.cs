using LiveCounter.Data;
using LiveCounter.Models;
using LiveCounter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(railwayPort, out var port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddDbContextFactory<LiveCounterDbContext>(options =>
    options.UseSqlite("Data Source=live-counter.db"));
builder.Services.AddSingleton<IPasswordHasher<LiveCounterUser>, PasswordHasher<LiveCounterUser>>();
builder.Services.AddSingleton<LivePresence>();
builder.Services.AddScoped<LiveCounterStore>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "livecounter.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.LoginPath = "/Account/Login";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "Live Counter API",
    Version = "v1",
    Description = "A small API for a persistent visit count and active sessions."
}));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<LiveCounterDbContext>();
    database.Database.EnsureCreated();
    database.Database.ExecuteSqlRaw("CREATE INDEX IF NOT EXISTS IX_Users_Email ON Users (Email)");
    database.EnsureCounterExists();
    database.EnsureRootUser(
        Environment.GetEnvironmentVariable("ROOT_EMAIL"),
        Environment.GetEnvironmentVariable("ROOT_PASSWORD"),
        scope.ServiceProvider.GetRequiredService<IPasswordHasher<LiveCounterUser>>());
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseSwagger();
app.UseSwaggerUI(options => options.DocumentTitle = "Live Counter API");
app.MapGet("/", (HttpContext context, IWebHostEnvironment environment) =>
    context.User.Identity?.IsAuthenticated == true
        ? Results.File(Path.Combine(environment.WebRootPath, "index.html"), "text/html")
        : Results.Redirect("/Account/Login"));
app.MapRazorPages();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
