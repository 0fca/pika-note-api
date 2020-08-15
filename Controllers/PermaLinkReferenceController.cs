using Microsoft.AspNetCore.Mvc;

namespace PikaNoteAPI.Controllers
{
    [ApiController]
    [Route("/perma/[action]")]
    public class PermaLinkReferenceController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok("Permalink controller.");
        }
    }
}