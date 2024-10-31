using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PikaNoteAPI.Domain.Contract;
using PikaNoteAPI.Infrastructure.Adapters.Http;

namespace PikaNoteAPI.Application.Extensions;

public static class ConfigureNotesStorageHttpClient
{
    public static IApplicationBuilder UseConfigureNotesStorageHttpClient(this IApplicationBuilder builder)
    {
        var notes = builder.ApplicationServices.GetRequiredService<INotes>();
        var httpClient = builder.ApplicationServices.GetRequiredService<NoteStorageHttpClient>();
        notes.ConfigureNoteStorageHttpClient(httpClient);
        return builder;
    }
}