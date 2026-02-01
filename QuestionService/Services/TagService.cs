using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QuestionService.Data;
using QuestionService.Models;

namespace QuestionService.Services
{
    public class TagService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly QuestionDbContext _db;
        private const string CacheKey = "tags";

        public TagService( IMemoryCache cache, QuestionDbContext db ) 
        { 
            _memoryCache = cache;
            _db = db;   
        }

        private async Task<List<Tag>>  GetTags()
        {
            return await _memoryCache.GetOrCreateAsync( CacheKey, async entry => 
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours( 2 );

                var tags = await _db.Tags.AsNoTracking().ToListAsync();

                return tags;

            }) ?? [];    
        }

        public async Task<bool> AreTagsValidAsync( List<string> slugs )
        {
            var tags =  await GetTags();
            var tagSet = tags.Select( t => t.Slug ).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return slugs.All( x => tagSet.Contains( x ) );
        }
    }
}
