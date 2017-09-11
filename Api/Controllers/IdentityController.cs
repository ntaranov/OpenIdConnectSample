using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers
{

    [Authorize]
    public class IdentityController : ControllerBase
    {
        [HttpGet]
        [Route("identity")]
        public IActionResult Get()
        {
            return new JsonResult(from c in User.Claims select new { c.Type, c.Value });
        }

        [HttpGet]
        [Route("superpowers")]
        [Authorize(Policy = "AdminsOnly")]
        public IActionResult Superpowers()
        {
            return new JsonResult("Superpowers!");
        }
    }
}
