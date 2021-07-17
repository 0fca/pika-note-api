using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PikaNoteAPI.Data;
using PikaNoteAPI.Models;
using PikaNoteAPI.Repositories;

namespace PikaNoteAPI.Services
{
    public class NoteService : INoteService
    {
        private readonly NoteRepository _noteRepository;

        public NoteService(NoteRepository noteRepository)
        {
            this._noteRepository = noteRepository;
        }

        public async Task<string> Add(NoteAddUpdateDto n)
        {
            var note = await _noteRepository.AddAsync(n.NewNote());
            return note.Id;
        }

        public async Task<bool> Remove(string id)
        {
            return (await this._noteRepository.DeleteAsync(id));
        }

        public async Task<bool> Update(NoteAddUpdateDto n, string id)
        {
            try
            {
                var currentNote = await GetNoteById(id);
                currentNote.Update(n.ToNote(id));
                await this._noteRepository.UpdateAsync(id, currentNote);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }

            return true;
        }

        public async Task RemoveLast()
        {
            await this._noteRepository.DeleteAsync(this.GetLatest().Id);
        }

        public Note GetLatest()
        {
            return this._noteRepository.GetRange(0, 1, 1).First();
        }

        public async Task<IList<Note>> FindByDate(DateTime d, IList<Note> notes)
        {
            return (await this._noteRepository.GetByDateAsync(d)).ToList();
        }

        public async Task<Note> GetNoteById(string id)
        {
            return await this._noteRepository.GetByIdAsync(id);
        }

        public IList<Note> GetNotes(int offset = 0, int pageSize = 10, int order = 0)
        {
            return this._noteRepository.GetRange(offset, pageSize, order).ToList();
        }
    }
}