using System.Collections.Generic;
using System.Threading.Tasks;

namespace PikaNoteAPI.Infrastructure.Services.Chat;

public interface IChatToolService
{
    Task<List<ChatToolDto>> GetAvailableToolsAsync();
    Task<bool> IsToolAllowedAsync(string toolName);
}

public record ChatToolDto
{
    public string Uid { get; init; } = "";
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public string Type { get; init; } = "";
}
