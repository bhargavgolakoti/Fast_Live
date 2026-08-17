using AspnetCoreMvcFull.Models.Api;
using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspnetCoreMvcFull.Controllers.Api;

[ApiController]
[Route("api/v1/visitors")]
[Produces("application/json")]
public class VisitorsApiController(VisitorCounterService visitorCounterService, VisitorPresenceService visitorPresenceService) : ControllerBase
{
    [HttpPost("visit")]
    public async Task<ActionResult<VisitorCountResponse>> RegisterVisit([FromQuery] string sessionId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > 100)
        {
            return BadRequest(new { error = "A valid sessionId is required." });
        }

        var isNewVisitor = visitorPresenceService.RecordHeartbeat(sessionId);
        var count = isNewVisitor
            ? await visitorCounterService.RegisterVisitAsync(cancellationToken)
            : await visitorCounterService.GetCountAsync(cancellationToken);
        return Ok(new VisitorCountResponse(count, visitorPresenceService.GetActiveUserCount(), DateTimeOffset.UtcNow));
    }

    [HttpGet("count")]
    public async Task<ActionResult<VisitorCountResponse>> GetCount(CancellationToken cancellationToken)
    {
        var count = await visitorCounterService.GetCountAsync(cancellationToken);
        return Ok(new VisitorCountResponse(count, visitorPresenceService.GetActiveUserCount(), DateTimeOffset.UtcNow));
    }
}