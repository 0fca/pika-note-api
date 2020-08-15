using System;
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

            var apiResponse = new ApiResponse
            {
                Success = true,
                Payload = await _noteService.GetNoteById(id)
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

        [HttpGet]
        [Route("list/filter/{date}")]
        public IActionResult FindByDate(DateTime date)
        {
            return StatusCode(501, new ApiResponse()
            {
                Message = "Feature not implemented",
                Success = true
            });
        }

        [HttpGet]
        [Route("list")]
        public IActionResult List()
        {
            return StatusCode(501, new ApiResponse()
            {
                Message = "Feature not implemented",
                Success = true
            });
        }
    }
}