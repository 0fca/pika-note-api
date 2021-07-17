using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PikaNoteAPI.Data;
using PikaNoteAPI.Models;

namespace PikaNoteAPI.Services
{
    public interface INoteService
    {
        Task<string> Add(NoteAddUpdateDto n);
        Task<bool> Remove(string id);

        Task<bool> Update(NoteAddUpdateDto n, string id);
        Task RemoveLast();
        Task<IList<Note>> FindByDate(DateTime d, IList<Note> notes = null);
        Task<Note> GetNoteById(string id);
        IList<Note> GetNotes(int offset, int pageSize, int order);
    }
}