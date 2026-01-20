using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestionService.Data;
using QuestionService.Models;

namespace QuestionService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class TagsController : ControllerBase
    {
        private readonly QuestionDbContext _db;

        public TagsController(QuestionDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<Tag>>> GetTags()
        {
            return await _db.Tags.OrderBy(t => t.Name).ToListAsync();
        }       
    }
}
