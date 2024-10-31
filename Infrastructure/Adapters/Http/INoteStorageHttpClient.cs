using System.Threading.Tasks;
using PikaNoteAPI.Infrastructure.Adapters.Http.DTO;

namespace PikaNoteAPI.Infrastructure.Adapters.Http;

public interface INoteStorageHttpClient
{
    public Task<AddNoteResultDTO?> CreateNoteObject(string token, string objectName, string bucketId, string humanName, string content);
    public Task<string?> GetNoteRawContentByIdAsUser(string token, string bucketId, string objectName);

}