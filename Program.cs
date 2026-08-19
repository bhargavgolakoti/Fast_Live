using LiveCounter.Data;
using LiveCounter.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var railwayPort = Environment.GetEnvironmentVariable("PORT");
if (int.TryParse(railwayPort, out var port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddDbContextFactory<LiveCounterDbContext>(options =>
    options.UseSqlite("Data Source=live-counter.db"));
builder.Services.AddSingleton<LivePresence>();
builder.Services.AddScoped<LiveCounterStore>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
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
    database.EnsureCounterExists();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI(options => options.DocumentTitle = "Live Counter API");
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();
