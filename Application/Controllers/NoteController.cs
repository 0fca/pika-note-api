using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using PikaNoteAPI.Domain.Contract;
using PikaNoteAPI.Domain.Models.DTO;
using PikaNoteAPI.Models;

namespace PikaNoteAPI.Application.Controllers
{
    [ApiController]
    [Route("/notes")]
    [Consumes("application/json")]
    [EnableCors("Base")]
    public class NotesController : Controller
    {
        private readonly INotes _notes;
        private readonly IBuckets _buckets;

        public NotesController(
            INotes notes,
            IBuckets buckets
            )
        {
            _notes = notes;
            _buckets = buckets;
        }

        [HttpGet]
        [Route("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> Index(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var token = HttpContext.Request.Cookies[".AspNet.Identity"]!;
            var note = await _notes.GetNoteByIdAsUser(token, id);
            
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
        [Route("/notes")]
        [Authorize(Roles = "Administrator, Moderator")]
        public async Task<IActionResult> Add(
            [FromBody] NoteAddUpdateDto? note,
            [FromQuery] string bucketId
            )
        {
            if (note == null)
            {
                return BadRequest("Note is null");
            }
            note.UpdateBucketId(bucketId);
            var apiResponse = new ApiResponse();
            try
            {
                var token = HttpContext.Request.Cookies[".AspNet.Identity"]!;
                var id = await _notes.Add(token, note);
                apiResponse.Success = true;
                apiResponse.Message = "Added note successfully";
                return Created($"/notes/{id}", apiResponse);
            }
            catch(AuthenticationException a)
            {
                apiResponse.Message = a.Message;
                apiResponse.Success = false;
                return StatusCode(401, apiResponse);
            }
        }
        
        [HttpDelete]
        [Route("{id?}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Remove(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest();
            }
            var apiResponse = new ApiResponse();
            try
            {
                if (!await _notes.Remove(id)) return NotFound();
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
        [Route("{id}")]
        [Authorize(Roles = "Moderator, Administrator")]
        public async Task<IActionResult> Update([FromBody]NoteAddUpdateDto? note, string id)
        {
            if (note == null)
            {
                return BadRequest();
            }

            var token = HttpContext.Request.Cookies[".AspNet.Identity"];
            if (!await _notes.UpdateNoteAsUser(note, id, token)) return NotFound();
            return Ok(new ApiResponse {Success = true, Message = "Updated note"});
        }

        [HttpGet]
        [Route("/notes/")]
        [AllowAnonymous]
        public async Task<IActionResult> List(
                            [FromQuery] string bucketId,
                            [FromQuery] int offset = 0, 
                            [FromQuery] int pageSize = 10, 
                            [FromQuery] int order = 0,
                            [FromQuery] string date = null
            )
        {
            var token = HttpContext.Request.Cookies[".AspNet.Identity"]!;
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }
            var notes = await _notes.GetNotesAsUser(token, bucketId, offset, pageSize, order);
            if (string.IsNullOrEmpty(date))
                return Ok(new ApiResponse
                {
                    Message = "All notes retrieved successfully",
                    Success = true,
                    Payload = notes
                });
            return Ok(new ApiResponse
            {
                Message = $"All notes by date {date} retrieved successfully",
                Success = true,
                Payload = await _notes.FindByDate(DateTime.Parse(date), notes)
            });
        }

        [HttpGet]
        [ActionName("buckets")]
        [Route("/[controller]/[action]")]
        [AllowAnonymous]
        public async Task<IActionResult> Buckets()
        {
            var token = HttpContext.Request.Cookies[".AspNet.Identity"]!;
            return Ok(
                new ApiResponse {
                    Success = true,
                    Message = "Buckets returned successfully",
                    Payload = await this._buckets.GetBucketsForTokenAsync(token)
                }
            ); 
        }
    }
}