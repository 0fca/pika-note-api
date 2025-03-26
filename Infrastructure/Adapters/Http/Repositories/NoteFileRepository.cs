using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PikaNoteAPI.Infrastructure.Adapters.Http.DTO;

namespace PikaNoteAPI.Infrastructure.Adapters.Http.Repositories;

// TODO: Move create methods to command section.
public class NoteFileRepository
{
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private NoteStorageHttpClient _noteStorageHttpClient;
        

    public NoteFileRepository(
        IConfiguration configuration
        )
    {
        this._configuration = configuration;
    }

    public async Task<string> GetNoteContentByIdAsUser(string token, string objectName, string bucketId)
    {
        var content = await this._noteStorageHttpClient.GetNoteRawContentByIdAsUser(token, bucketId, objectName);
        if (string.IsNullOrEmpty(content))
        {
            throw new AggregateException("There is no note returned from storage");
        }
        return content;
    }

    public async Task<AddNoteResultDTO?> CreateNoteObject(string token, string objectName, string bucketId, string humanName, string content)
    {
        return await this._noteStorageHttpClient.CreateNoteObject(token, objectName, bucketId, humanName, content);
    }

    public async Task<UpdateNoteResultDTO?> UpdateNoteObject(string token, string objectName, string bucketId, string humanName, string content)
    {
        return await this._noteStorageHttpClient.UpdateNoteObject(token, objectName, bucketId, humanName,content);
    }

    public void ConfigureNoteStorageHttpClient(NoteStorageHttpClient noteStorageHttpClient)
    {
        this._noteStorageHttpClient = noteStorageHttpClient;
    }
}