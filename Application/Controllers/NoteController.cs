using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using PikaNoteAPI.Application.Filters;
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
        //[PikaCoreAuthorize]
        [Authorize(Policy =  "AdministratorOrModerator")]
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
                apiResponse.Payload = new { 
                    Id = id
                };
                return Created($"/notes/{id}", apiResponse);
            }
            catch(AuthenticationException a)
            {
                apiResponse.Message = a.Message;
                apiResponse.Success = false;
                return StatusCode(401, apiResponse);
            }
            catch(Exception ex)
            {
                apiResponse.Message = "Couldn't add note: " + ex.Message;
                apiResponse.Success = false;
                return StatusCode(500, apiResponse);
            }
        }
        
        [HttpDelete]
        [Route("{id?}")]
        [PikaCoreAuthorize]
        [Authorize(Policy =  "AdministratorOrModerator")]
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
        [PikaCoreAuthorize]
        [Authorize(Policy =  "AdministratorOrModerator")]
        public async Task<IActionResult> Update([FromBody]NoteAddUpdateDto? note, string id)
        {
            if (note == null)
            {
                return BadRequest();
            }

            var token = HttpContext.Request.Cookies[".AspNet.Identity"];
            if (!await _notes.UpdateNoteAsUser(note, id, token)) return NotFound();
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Updated note",
                Payload = new
                {
                    Id = id
                }
            });
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
        [Route("/notes/search")]
        [Authorize(Policy =  "AdministratorOrModerator")]
        public async Task<IActionResult> Search(
            [FromQuery] string bucketId,
            [FromQuery] string query,
            [FromQuery] int maxResults = 20
        )
        {
            var token = HttpContext.Request.Cookies[".AspNet.Identity"]!;
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }
            if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(bucketId))
            {
                return BadRequest(new ApiResponse { Success = false, Message = "Query and bucketId are required" });
            }
            var notes = await _notes.SearchNotes(token, bucketId, query, maxResults);
            return Ok(new ApiResponse
            {
                Success = true,
                Message = "Search results",
                Payload = notes
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