using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PikaNoteAPI.Data;

namespace PikaNoteAPI.Services
{
    public class NoteService : INoteService
    {
        private readonly MainDbContext _main;

        public NoteService(MainDbContext main)
        {
            _main = main;
        }
        
        public async Task<int> Add(Note n)
        {
            var note = _main.Notes.Update(n);
            await _main.SaveChangesAsync();
            return note.Entity.Id;
        }

        public async Task Remove(Note n)
        {
            n = await _main.Notes.FindAsync(n.Id);
            _main.Notes.Remove(n);
            await _main.SaveChangesAsync();
        }

        public async Task RemoveLast()
        {
            var note = _main.Notes.OrderByDescending(n => n.Timestamp).First();
            _main.Notes.Remove(note);
            await _main.SaveChangesAsync();
        }

        public async Task<IList<Note>> FindByDate(DateTime d)
        {
            throw new NotImplementedException();
        }

        public async Task<Note> GetNoteById(int? id)
        {
            return await _main.Notes.FindAsync(id);
        }

        public async Task<IList<Note>> GetAllNotes()
        {
            throw new NotImplementedException();
        }
    }
}