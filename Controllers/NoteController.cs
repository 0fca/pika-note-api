using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PikaNoteAPI.Data;
using PikaNoteAPI.Models;
using PikaNoteAPI.Services;

namespace PikaNoteAPI.Controllers
{
    [ApiController]
    [Route("/")]
    [Consumes("application/json")]
    public class NoteController : Controller
    {
        private readonly INoteService _noteService;
        public NoteController(INoteService noteService)
        {
            _noteService = noteService;
        }

        [HttpGet]
        [Route("{id?}")]
        public async Task<IActionResult> Index(int? id)
        {
            if (id == null)
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
                apiResponse.Message = "Added note successfully.";
                return Created($"/{id}", apiResponse);
            }
            catch
            {
                apiResponse.Message = "Some error occurred while adding the note.";
                apiResponse.Success = false;
                return StatusCode(500, apiResponse);
            }
        }
        
        [HttpDelete]
        [Route("{id}/remove")]
        public async Task<IActionResult> Remove(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            var apiResponse = new ApiResponse();
            try
            {
                if (!await _noteService.Remove(id)) return NotFound();
                
                apiResponse.Message = "Successfully deleted note.";
                return Ok(apiResponse);

            }
            catch(Exception ex)
            {
                Console.Write(ex.Message);
                apiResponse.Success = false;
                apiResponse.Message = "Couldn't remove note.";
                return StatusCode(500, apiResponse);
            }
        }

        [HttpGet]
        [Route("notes/{date}")]
        public async Task<IActionResult> FindByDate(string date)
        {
            var dateTime = DateTime.Parse(date);
            return Ok(new ApiResponse()
            {
                Success = true,
                Payload = await _noteService.FindByDate(dateTime)
            });
        }

        [HttpGet]
        [Route("notes")]
        public async Task<IActionResult> List([FromQuery] int order = 0, 
            [FromQuery] int count = 10)
        {
            return Ok(new ApiResponse()
            {
                Message = "All notes retrieved successfully.",
                Success = true,
                Payload = await _noteService.GetNotes(order, count)
            });
        }
    }
}