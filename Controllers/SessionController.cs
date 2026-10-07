using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Pidar.Controllers
{
    /// <summary>
    /// Called by wwwroot/js/idle-logout.js while the user is active (or clicks "Stay signed in"),
    /// so the sign-in cookie's sliding expiration is renewed even when they only read or scroll.
    /// </summary>
    [Authorize]
    [Route("session")]
    public class SessionController : Controller
    {
        [HttpGet("keepalive")]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult KeepAlive() => NoContent();
    }
}
