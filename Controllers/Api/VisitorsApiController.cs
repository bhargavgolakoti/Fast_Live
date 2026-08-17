using AspnetCoreMvcFull.Models.Api;
using AspnetCoreMvcFull.Services;
using Microsoft.AspNetCore.Mvc;

namespace AspnetCoreMvcFull.Controllers.Api;

[ApiController]
[Route("api/v1/visitors")]
[Produces("application/json")]
public class VisitorsApiController(VisitorCounterService visitorCounterService) : ControllerBase
{
    [HttpPost("visit")]
    public async Task<ActionResult<VisitorCountResponse>> RegisterVisit(CancellationToken cancellationToken)
    {
        var count = await visitorCounterService.RegisterVisitAsync(cancellationToken);
        return Ok(new VisitorCountResponse(count, DateTimeOffset.UtcNow));
    }

    [HttpGet("count")]
    public async Task<ActionResult<VisitorCountResponse>> GetCount(CancellationToken cancellationToken)
    {
        var count = await visitorCounterService.GetCountAsync(cancellationToken);
        return Ok(new VisitorCountResponse(count, DateTimeOffset.UtcNow));
    }
}