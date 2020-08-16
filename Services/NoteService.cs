using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
            var note = await _main.Notes.AddAsync(n);
            await _main.SaveChangesAsync();
            return note.Entity.Id;
        }

        public async Task<bool> Remove(int? id)
        {
            var note = await _main.Notes.FindAsync(id);
            if (note == null)
            {
                return false;
            }
            _main.Notes.Remove(note);
            await _main.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Update(Note n)
        {
            try
            {
                var note = await GetNoteById(n.Id);
                note.Name = n.Name;
                note.Content = n.Content;
                note.Timestamp = DateTime.Now;
                _main.Notes.Update(note);
                await _main.SaveChangesAsync();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        public async Task RemoveLast()
        {
            var note = _main.Notes.OrderByDescending(n => n.Timestamp).First();
            _main.Notes.Remove(note);
            await _main.SaveChangesAsync();
        }

        public async Task<IList<Note>> FindByDate(DateTime d)
        {
            return await _main.Notes.Where(n => n.Timestamp.Date.Equals(d.Date)).ToListAsync();
        }

        public async Task<Note> GetNoteById(int? id)
        {
            return await _main.Notes.FindAsync(id);
        }

        public async Task<IList<Note>> GetNotes(int order, int count)
        {
            var noteList = _main.Notes.OrderBy(n => n.Timestamp).AsQueryable();

            if (order == 1)
            { 
                noteList = noteList.OrderByDescending(n => n.Id);
            }

            noteList = noteList.Take(count);
            return await noteList.ToListAsync();
        }
    }
}