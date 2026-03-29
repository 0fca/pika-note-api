using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pika.Domain.Notes.Data;
using PikaNoteAPI.Domain.Models.DTO;
using PikaNoteAPI.Infrastructure.Adapters.Http;
using PikaNoteAPI.Infrastructure.Services.Security;

namespace PikaNoteAPI.Domain.Contract
{
    public interface INotes
    {
        Task<string> Add(string token, NoteAddUpdateDto? n);
        Task<bool> Remove(string id);
        Task<bool> UpdateNoteAsUser(NoteAddUpdateDto n, string id, string token);
        Task<IList<Note>> FindByDate(DateTime d, IList<Note> notes = null);
        Task<NoteDTO?> GetNoteByIdAsUser(string token, string id);
        Task<IList<Note>> GetNotesAsUser(string token, string bucketId, int offset, int pageSize, int order);
        Task<IList<Note>> SearchNotes(string token, string bucketId, string query, int maxResults = 20, int page = 1);
        bool HasSecurityConfigured();
        internal void ConfigureSecurityService(ISecurityService securityService);
        internal void ConfigureNoteStorageHttpClient(NoteStorageHttpClient noteStorageHttpClient);
    }
}