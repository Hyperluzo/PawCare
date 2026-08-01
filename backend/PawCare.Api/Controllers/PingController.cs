using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PawCare.Api.Data;

namespace PawCare.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PingController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PingController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Get() => Ok(new { status = "ok", time = DateTime.UtcNow });

        [HttpGet("db")]
        public async Task<IActionResult> CheckDb()
        {
            bool canConnect = await _db.Database.CanConnectAsync();
            return Ok(new { databaseConnected = canConnect });
        }

        [HttpGet("secure")]
        [Authorize]
        public IActionResult GetSecure() => Ok(new { message = "You're authenticated!" });
    }
}