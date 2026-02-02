using Contracts;
using FastExpressionCompiler;
using JasperFx.CodeGeneration.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using QuestionService.Data;
using QuestionService.DTOs;
using QuestionService.Models;
using QuestionService.Services;
using System.Security.Claims;
using Wolverine;
using Wolverine.Persistence;

namespace QuestionService.Controllers
{
    // [Route("api/[controller]")]
    [Route("[controller]")]
    [ApiController]
    public class QuestionsController : ControllerBase
    {
        private readonly QuestionDbContext _db;
        private readonly IMessageBus _bus;
        private readonly TagService _tagService;

        public QuestionsController(QuestionDbContext db, IMessageBus bus, TagService tagService)
        {
            _db = db;
            _bus = bus;
            _tagService = tagService;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Question>> CreateQuestion(CreateQuestionDto dto)
        {
            //// Placeholder implementation
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //var validTags = await _db.Tags.Where(t => dto.Tags.Contains(t.Slug)).Select(t => t.Slug).ToListAsync();

            //var missing = dto.Tags.Except(validTags).ToList();

            //if (missing.Count != 0)
            //{
            //    return BadRequest($"The following tags are invalid: {string.Join(", ", missing)}");
            //}

            if (!await _tagService.AreTagsValidAsync(dto.Tags))
            {
                return BadRequest("Invalid Tags");
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

            await _bus.PublishAsync(new QuestionCreated(question.Id, question.Title, question.Content, question.CreatedAt, question.TagSlugs));

            return Created($"/questions/{question.Id}", question);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<Question>>> GetQuestions(string? tag)
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
            //var question = await _db.Questions.FindAsync(id);

            //if (question == null)
            //{
            //    return NotFound();
            //}

            //await _db.Questions.ExecuteUpdateAsync(q => q.SetProperty(q => q.ViewCount, q => q.ViewCount + 1));

            //return question;

            var question = await _db.Questions
               .Include(x => x.Answers)
               .FirstOrDefaultAsync(x => x.Id == id);

            if (question is null) return NotFound();

            await _db.Questions.Where(x => x.Id == id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ViewCount, x => x.ViewCount + 1));

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

            //var validTags = await _db.Tags.Where(t => dto.Tags.Contains(t.Slug)).Select(t => t.Slug).ToListAsync();

            //var missing = dto.Tags.Except(validTags).ToList();

            //if (missing.Count != 0)
            //{
            //    return BadRequest($"The following tags are invalid: {string.Join(", ", missing)}");
            //}

            if (!await _tagService.AreTagsValidAsync(dto.Tags))
            {
                return BadRequest("Invalid Tags");
            }

            question.Title = dto.Title;
            question.Content = dto.Content;
            question.UpdatedAt = DateTime.UtcNow;
            question.TagSlugs = dto.Tags;

            await _db.SaveChangesAsync();

            // he has AsArray 
            await _bus.PublishAsync(new QuestionUpdated(question.Id, question.Title, question.Content, question.TagSlugs.ToList()));

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

            await _bus.PublishAsync(new QuestionDeleted(question.Id));

            return NoContent();
        }


        // Answers 
        [Authorize]
        [HttpPost("{questionId}/answers")]
        public async Task<ActionResult> PostAnswer(string questionId, CreateAnswerDto dto)
        {
            var question = await _db.Questions.FindAsync(questionId);

            if (question == null)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var name = User.FindFirstValue("name");

            if (userId is null || name is null)
            {
                return BadRequest("Can not get user details");
            }

            var answer = new Answer
            {
                UserId = userId,
                QuestionId = questionId,
                Content = dto.Content,
                UserDisplayName = name,
                CreatedAt = DateTime.UtcNow,
            };

            _db.Answers.Add(answer);

            question.AnswerCount += 1;

            await _db.SaveChangesAsync();

            await _bus.PublishAsync(new UpdatedAnswerCount(questionId, question.AnswerCount));

            return Created($"/questions/{questionId}", answer );

        }

        [Authorize]
        [HttpPut("{questionId}/answers/{answerId}")]
        public async Task<ActionResult> UpdateAnswer(string questionId, string answerId, CreateAnswerDto dto)
        {
            var answer = await _db.Answers.FindAsync(answerId);
            if (answer is null) return NotFound();
            if (answer.QuestionId != questionId) return BadRequest("Cannot update answer details");

            answer.Content = dto.Content;
            answer.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return NoContent();

        }

        [Authorize]
        [HttpDelete("{questionId}/answers/{answerId}")]
        public async Task<ActionResult> DeleteAnswer(string questionId, string answerId)
        {
            var answer = await _db.Answers.FindAsync(answerId);

            var question = await _db.Questions.FindAsync(questionId);

            if (answer is null || question is null) return NotFound();

            if (answer.QuestionId != questionId || answer.Accepted) return BadRequest("Cannot delete this answer");

            _db.Answers.Remove(answer);

            question.AnswerCount--;

            await _db.SaveChangesAsync();

            await _bus.PublishAsync(new UpdatedAnswerCount(questionId, question.AnswerCount));

            return NoContent();
        }



        [Authorize]
        [HttpPost("questions/{questionId}/answer/{answerId}/accept")]
        public async Task<ActionResult> AcceptAnswer(string questionId, string answerId)
        {
            var answer = await _db.Answers.FindAsync(answerId);

            var question = await _db.Questions.FindAsync(questionId);

            if (answer is null || question is null) return NotFound();

            if (answer.QuestionId != questionId || question.HasAcceptedAnswer) return BadRequest("Cannot accept answer");
            
            answer.Accepted = true;
            
            question.HasAcceptedAnswer = true;

            await _db.SaveChangesAsync();

            await _bus.PublishAsync(new AnswerAccepted(questionId));

            return NoContent();
        }
    }
}