using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using PikaNoteAPI.Data;
using PikaNoteAPI.Models;
using PikaNoteAPI.Services;

namespace PikaNoteAPI.Controllers
{
    [ApiController]
    [Route("/notes")]
    [Consumes("application/json")]
    [EnableCors("Base")]
    public class NoteController : Controller
    {
        private readonly INoteService _noteService;
        public NoteController(INoteService noteService)
        {
            _noteService = noteService;
        }

        [HttpGet]
        [Route("{id}/view")]
        public async Task<IActionResult> Index(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var note = await _noteService.GetNoteById(id);
            if (note == null)
            {
                return NotFound();
            }
            var apiResponse = new ApiResponse
            {
                Success = true,
                Payload = note
            };
            return Ok(apiResponse);
        }

        [HttpPost]
        [Route("add")]
        public async Task<IActionResult> Add([FromBody] Note note)
        {
            if (note == null)
            {
                return BadRequest();
            }
            var apiResponse = new ApiResponse();
            try
            {
                var id = await _noteService.Add(note);
                apiResponse.Success = true;
                apiResponse.Message = "Added note successfully";
                return Created($"/{id}", apiResponse);
            }
            catch
            {
                apiResponse.Message = "Some error occurred while adding the note";
                apiResponse.Success = false;
                return StatusCode(500, apiResponse);
            }
        }
        
        [HttpDelete]
        [Route("{id?}/remove")]
        public async Task<IActionResult> Remove(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }
            var apiResponse = new ApiResponse();
            try
            {
                if (!await _noteService.Remove(id)) return NotFound();
                apiResponse.Message = "Successfully deleted note";
                return Ok(apiResponse);
            }
            catch(Exception ex)
            {
                apiResponse.Success = false;
                apiResponse.Message = "Couldn't remove note";
                return StatusCode(500, apiResponse);
            }
        }
        
        [HttpPut]
        [Route("{id}/update")]
        public async Task<IActionResult> Update([FromBody]Note note, string id)
        {
            if (note == null)
            {
                return BadRequest();
            }
            note.Id = id;
            if (!await _noteService.Update(note)) return NotFound();
            return Ok(new ApiResponse {Success = true, Message = "Updated note"});
        }

        [HttpGet]
        [Route("{date}")]
        public async Task<IActionResult> FindByDate(string date)
        {
            try
            {
                var dateTime = DateTime.Parse(date);
                return Ok(new ApiResponse()
                {
                    Success = true,
                    Payload = await _noteService.FindByDate(dateTime)
                });
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpGet]
        [Route("/notes/")]
        public IActionResult List(
                            [FromQuery] int offset = 0, 
                            [FromQuery] int pageSize = 10, 
                            [FromQuery] int order = 0
            )
        {
            return Ok(new ApiResponse()
            {
                Message = "All notes retrieved successfully",
                Success = true,
                Payload = _noteService.GetNotes(offset, pageSize, order)
            });
        }
    }
}