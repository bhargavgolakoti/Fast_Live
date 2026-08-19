using LiveCounter.Models;
using LiveCounter.Services;
using Microsoft.AspNetCore.Mvc;

namespace LiveCounter.Api;

[ApiController]
[Route("api/v1/live-counter")]
[Produces("application/json")]
public class LiveCounterController(LiveCounterStore store, LivePresence presence) : ControllerBase
{
    [HttpPost("visit")]
    public async Task<ActionResult<LiveCounterSnapshot>> Visit([FromQuery] string? sessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > 100)
        {
            return BadRequest(new { error = "A sessionId between 1 and 100 characters is required." });
        }

        var isNewSession = presence.Touch(sessionId);
        var totalVisits = isNewSession
            ? await store.AddVisitAsync(cancellationToken)
            : await store.ReadAsync(cancellationToken);

        return Ok(CreateSnapshot(totalVisits));
    }

    [HttpPost("leave")]
    public IActionResult Leave([FromQuery] string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > 100)
        {
            return BadRequest(new { error = "A sessionId between 1 and 100 characters is required." });
        }

        presence.Remove(sessionId);
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<LiveCounterSnapshot>> Get(CancellationToken cancellationToken)
    {
        return Ok(CreateSnapshot(await store.ReadAsync(cancellationToken)));
    }

    private LiveCounterSnapshot CreateSnapshot(long totalVisits) =>
        new(totalVisits, presence.Count, DateTimeOffset.UtcNow);
}
