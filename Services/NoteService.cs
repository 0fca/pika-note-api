using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pika.Adapters.Persistence.Note.Repositories;
using Pika.Domain.Note.DTO;
using Pika.Domain.Notes.Data;
using PikaNoteAPI.Exceptions;
using PikaNoteAPI.Services.Security;

namespace PikaNoteAPI.Services
{
    public class NoteService : INoteService
    {
        private readonly NoteRepository _noteRepository;
        private ISecurityService? _securityService;
        public NoteService(NoteRepository noteRepository, 
                           ISecurityService? service = null)
        {
            this._noteRepository = noteRepository;
            if (service == null) return;
            this._securityService = service;
            this._securityService
                .VerifyRemoteClientWithClientId("note-api-dev").Wait();
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
        
        public async Task<Note> GetLatest(string bucketId)
        { 
            var note = this._noteRepository.GetRange(bucketId, 0, 1, 1).First();
            if (!this.HasSecurityConfigured()) return note;
            var nid = Guid.Parse(note.Id);

            var dic = new Dictionary<Guid, Guid>
            {
                { nid, Guid.Parse(bucketId) }
            };
            if (!(await _securityService.HasNotesAccess(dic))[nid])
            {
                throw new IllegalNoteFileAccess();
            }
            return note;
        }

        public async Task<IList<Note>> FindByDate(DateTime d, IList<Note> notes)
        {
            return (await this._noteRepository.GetByDateAsync(d)).ToList();
        }

        public async Task<Note> GetNoteById(string id)
        {
            return await this._noteRepository.GetByIdAsync(id);
        }

        public async Task<IList<Note>> GetNotes(string? bucketId = null, int offset = 0, int pageSize = 10, int order = 0)
        {
            if (string.IsNullOrEmpty(bucketId))
            {
                return new List<Note>();
            }
            var notes = this._noteRepository.GetRange(bucketId, offset, pageSize, order).AsQueryable();
            var ids = notes.Select(n => n.Id).ToList();
            if (_securityService == null) return notes.ToList();
            var notesWithBids = new Dictionary<Guid, Guid>();
            ids.ForEach(id =>
            {
                notesWithBids.Add(Guid.Parse(id), Guid.Parse(bucketId));
            });
            var notesIds = await _securityService.HasNotesAccess(notesWithBids);
            if (notesIds == null)
            {
                throw new IllegalNoteFileAccess();
            }

            notes.ToList().RemoveAll(n =>
                notesIds.ContainsKey(Guid.Parse(n.Id)) && notesIds[Guid.Parse(n.Id)]
            );
            return notes.ToList();
        }

        public void ConfigureSecurityService(ISecurityService securityService)
        {
            this._securityService = securityService;
        }

        public bool HasSecurityConfigured()
        {
            return this._securityService != null;
        }
    }
}