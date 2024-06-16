using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PikaNoteAPI.Services;
using PikaNoteAPI.Services.Security;

namespace PikaNoteAPI.Middlewares;

public class NoteFileStorageSecurity
{
    private readonly RequestDelegate _next;
    private readonly ISecurityService _securityService;
    private readonly INoteService _noteService;
    
    public NoteFileStorageSecurity(
        INoteService noteService,
        ISecurityService securityService, RequestDelegate next)
    {
        this._securityService = securityService;
        _next = next;
        this._noteService = noteService;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_noteService.HasSecurityConfigured())
        {
            _noteService.ConfigureSecurityService(this._securityService);
        }

        await _next(context);
    }
}