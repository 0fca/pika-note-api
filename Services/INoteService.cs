using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PikaNoteAPI.Data;

namespace PikaNoteAPI.Services
{
    public interface INoteService
    {
        Task<int> Add(Note n);
        Task Remove(Note n);
        Task RemoveLast();
        Task<IList<Note>> FindByDate(DateTime d);
        Task<Note> GetNoteById(int? id);
        Task<IList<Note>> GetAllNotes();
    }
}