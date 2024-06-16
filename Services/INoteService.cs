using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pika.Domain.Note.DTO;
using Pika.Domain.Notes.Data;
using PikaNoteAPI.Models;
using PikaNoteAPI.Services.Security;

namespace PikaNoteAPI.Services
{
    public interface INoteService
    {
        Task<string> Add(NoteAddUpdateDto n);
        Task<bool> Remove(string id);

        Task<bool> Update(NoteAddUpdateDto n, string id);
        Task<IList<Note>> FindByDate(DateTime d, IList<Note> notes = null);
        Task<Note> GetNoteById(string id);
        Task<IList<Note>> GetNotes(string bucketId, int offset, int pageSize, int order);
        bool HasSecurityConfigured();
        internal void ConfigureSecurityService(ISecurityService securityService);
    }
}