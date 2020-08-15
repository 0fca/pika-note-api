using Microsoft.AspNetCore.Mvc;

namespace PikaNoteAPI.Controllers
{
    [ApiController]
    [Route("/")]
    [Consumes("application/json")]
    public class NoteController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok("Notes controller.");
        }
    }
}