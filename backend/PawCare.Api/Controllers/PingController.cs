using Microsoft.AspNetCore.Mvc;

namespace PawCare.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new { status = "ok", time = DateTime.UtcNow });
    }
}