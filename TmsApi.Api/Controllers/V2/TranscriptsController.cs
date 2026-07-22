using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v2/transcripts")]
public class TranscriptsController : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("transcripts")]
    public async Task<IActionResult> RequestTranscript([FromBody] object? _, CancellationToken ct)
    {
        // Stub: Exercise 5 swaps this for enqueue + 202 + Location.
        await Task.Delay(TimeSpan.FromSeconds(2), ct);
        return Ok();
    }
}
