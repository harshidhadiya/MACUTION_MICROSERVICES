using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VERIFY.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VerifyController : ControllerBase
    {
        [HttpPost("verify")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> verifyProduct()
        {

          
          return Ok("DONE");
        }
    }
}