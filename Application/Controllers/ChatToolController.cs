using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PikaNoteAPI.Application.Filters;
using PikaNoteAPI.Infrastructure.Services.Chat;

namespace PikaNoteAPI.Application.Controllers;

[Route("[controller]")]
public class ChatToolController : Controller
{
    private readonly IChatToolService _chatToolService;

    public ChatToolController(IChatToolService chatToolService)
    {
        _chatToolService = chatToolService;
    }

    [PikaCoreAuthorize]
    [HttpGet]
    [Route("[action]")]
    public async Task<IActionResult> Available()
    {
        var tools = await _chatToolService.GetAvailableToolsAsync();
        return Ok(new { tools });
    }

    [PikaCoreAuthorize]
    [HttpPost]
    [Route("[action]")]
    public async Task<IActionResult> Validate([FromBody] ValidateToolRequest request)
    {
        var allowed = await _chatToolService.IsToolAllowedAsync(request.Tool);
        if (!allowed)
        {
            return BadRequest(new { message = $"Tool '{request.Tool}' is not available" });
        }
        return Ok(new { valid = true });
    }
}

public record ValidateToolRequest
{
    public string Tool { get; init; } = "";
}
