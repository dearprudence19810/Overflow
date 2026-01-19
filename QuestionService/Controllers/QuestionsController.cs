using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.DTOs;
using QuestionService.Models;
using System.Security.Claims;

namespace QuestionService.Controllers
{
    // [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly QuestionDbContext _db;

        public QuestionsController(QuestionDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Question>> CreateQuestion(CreateQuestionDto dto)
        {
            var validTags = await _db.Tags.Where(t => dto.Tags.Contains(t.Slug)).Select(t => t.Slug).ToListAsync();

            var missing = dto.Tags.Except(validTags).ToList();

            if (missing.Count != 0)
            {
                return BadRequest($"The following tags are invalid: {string.Join(", ", missing)}");
            }

            // Placeholder implementation
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (missing.Any())
            {
                return BadRequest($"The following tags are invalid: {string.Join(", ", missing)}");
            }


            var name = User.FindFirstValue("name");

            if (userId is null || name is null)
            {
                return BadRequest("Can not get user details");
            }

            var question = new Question
            {
                Title = dto.Title,
                Content = dto.Content,
                AskerId = userId,
                AskerDisplayName = name,
                CreatedAt = DateTime.UtcNow,
                ViewCount = 0,
                Votes = 0,
                HasAcceptedAnswer = false,
                TagSlugs = dto.Tags
            };

            _db.Questions.Add(question);
            await _db.SaveChangesAsync();

            return Created($"/questions/{question.Id}", question);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<Question>>> GetQuestions( string? tag )
        {
            var query = _db.Questions.AsQueryable();
            
            if (!string.IsNullOrEmpty(tag))
            {
                query = query.Where(q => q.TagSlugs.Contains(tag));
            }   

            return await query.OrderByDescending(q => q.CreatedAt).ToListAsync();
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Question>> GetQuestion(string id)
        {
            var question = await _db.Questions.FindAsync(id);

            if (question == null)
            {
                return NotFound();
            }

            await _db.Questions.ExecuteUpdateAsync(q => q.SetProperty(q => q.ViewCount, q => q.ViewCount + 1));   

            return question;
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<Question>> UpdateQuestion(string id, CreateQuestionDto dto)
        {
            var question = await _db.Questions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (question.AskerId != userId)
            {
                return Forbid();
            }

            var validTags = await _db.Tags.Where(t => dto.Tags.Contains(t.Slug)).Select(t => t.Slug).ToListAsync();

            var missing = dto.Tags.Except(validTags).ToList();

            if (missing.Count != 0)
            {
                return BadRequest($"The following tags are invalid: {string.Join(", ", missing)}");
            }

            question.Title = dto.Title;
            question.Content = dto.Content;
            question.UpdatedAt = DateTime.UtcNow;
            question.TagSlugs = dto.Tags;

            await _db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> DeleteQuestion(string id)
        {
            var question = await _db.Questions.FindAsync(id);

            if (question == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (question.AskerId != userId)
            {
                return Forbid();
            }

            _db.Questions.Remove(question);

            await _db.SaveChangesAsync();

            return NoContent();
        }   
    }
}
