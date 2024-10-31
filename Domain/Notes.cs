using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pika.Domain.Notes.Data;
using PikaNoteAPI.Adapters.Database.Note.Repositories;
using PikaNoteAPI.Domain.Contract;
using PikaNoteAPI.Domain.Models.DTO;
using PikaNoteAPI.Exceptions;
using PikaNoteAPI.Infrastructure.Adapters.Http;
using PikaNoteAPI.Infrastructure.Adapters.Http.Repositories;
using PikaNoteAPI.Infrastructure.Services.Security;

namespace PikaNoteAPI.Domain
{
    public class Notes : INotes
    {
        private readonly NoteRepository _noteRepository;
        private readonly NoteFileRepository _noteFileRepository;
        private ISecurityService? _securityService;
        public Notes(NoteRepository noteRepository, 
                    NoteFileRepository noteFileRepository
            )
        {
            this._noteRepository = noteRepository;
            this._noteFileRepository = noteFileRepository;
        }

        public async Task<string?> Add(
            string token, 
            NoteAddUpdateDto? n
            )
        {
            var noteObject = n.NewNote();
            var result = await _noteFileRepository.CreateNoteObject(token, noteObject.Id, noteObject.BucketId, noteObject.HumanName, n.Content);
            if (result == null)
            {
                return null;
            }

            noteObject.UpdateMachineName(result.MachineName);
            var note = await _noteRepository.AddAsync(noteObject);
                
            return note.Id;
        }

        public async Task<bool> Remove(string id)
        {
            return (await this._noteRepository.DeleteAsync(id));
        }

        public async Task<bool> UpdateNoteAsUser(NoteAddUpdateDto n, string id, string token)
        {
            try
            {
                var currentNote = await GetNoteByIdAsUser(token, id);
                if (currentNote == null)
                {
                    return false;
                }

                var note = n.ToNote(id);
                var result = await this._noteFileRepository.UpdateNoteObject(
                    token,
                    currentNote.MachineName,
                    currentNote.BucketId,
                    n.Name,
                    n.Content
                );
                if (result == null) { return false; }
                note.UpdateBucketId(result.BucketId);
                note.UpdateMachineName(result.MachineName);
                note.UpdateHumanName(n.Name);
                await this._noteRepository.UpdateAsync(id, note);
                
                return result.BucketId == currentNote.BucketId && result.MachineName == currentNote.MachineName;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

        public async Task<IList<Note>> FindByDate(DateTime d, IList<Note> notes)
        {
            return (await this._noteRepository.GetByDateAsync(d)).ToList();
        }

        public async Task<NoteDTO?> GetNoteByIdAsUser(string token, string id)
        {
            var note = await this._noteRepository.FindByIdAsync(id);
            if (note == null)
            {
                return null;
            }

            try
            {
                var content = await this._noteFileRepository
                    .GetNoteContentByIdAsUser(token, note.MachineName, note.BucketId);
                return NoteDTO.CreateFromNote(note, content);
            }
            catch (Exception e)
            {
                throw new ApplicationException($"An exception ocurred while downloading note: {e.Message}");
            }
        }

        public async Task<IList<Note>> GetNotesAsUser(string token, string? bucketId = null, int offset = 0, int pageSize = 10, int order = 0)
        {
            if (string.IsNullOrEmpty(bucketId))
            {
                return [];
            }
            var notes = this._noteRepository.GetRange(bucketId, offset, pageSize, order).AsQueryable();
            var ids = notes.Select(n => n.Id).ToList();
            if (_securityService == null) throw new IllegalNoteFileAccess();
            var notesWithBids = new Dictionary<Guid, Guid>();
            ids.ForEach(id =>
            {
                notesWithBids.Add(Guid.Parse(id), Guid.Parse(bucketId));
            });
            var notesIds = await _securityService.HasNotesAccess(token, notesWithBids);
            if (notesIds != null)
            {
                var notesAsList = notes.ToList();
                notesAsList.RemoveAll(n =>
                    notesIds.ContainsKey(Guid.Parse(n.Id)) && notesIds[Guid.Parse(n.Id)] == false
                );
                return notesAsList;
            }

            throw new ApplicationException("It appears that something failed while downloading notes");
        }

        public void ConfigureSecurityService(ISecurityService securityService)
        {
            this._securityService = securityService;
        }

        public void ConfigureNoteStorageHttpClient(NoteStorageHttpClient noteStorageHttpClient)
        {
           this._noteFileRepository.ConfigureNoteStorageHttpClient(noteStorageHttpClient); 
        }

        public bool HasSecurityConfigured()
        {
            return this._securityService != null;
        }
    }
}