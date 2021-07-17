using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PikaNoteAPI.Data;

namespace PikaNoteAPI.Services
{
    public interface INoteService
    {
        Task<string> Add(Note n);
        Task<bool> Remove(string id);

        Task<bool> Update(Note n);
        Task RemoveLast();
        Task<IList<Note>> FindByDate(DateTime d, IList<Note> notes = null);
        Task<Note> GetNoteById(string id);
        IList<Note> GetNotes(int offset, int pageSize, int order);
    }
}