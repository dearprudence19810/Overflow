using Contracts;
using SearchService.Models;
using Typesense;
using Wolverine.Runtime;
using Wolverine.Runtime.Handlers;

namespace SearchService.MessageHandlers
{
    public class QuestionCreatedHandler(ITypesenseClient client) 
    {
        public async Task HandleAsync(QuestionCreated message )
        {
            var created = new DateTimeOffset(message.Created).ToUnixTimeSeconds();

            var document = new SearchQuestion()
            {
                Id = message.QuestionId,
                Title = message.Title,
                Content = StripHtml(message.Content),
                CreatedAt = created,
                Tags = message.Tags.ToArray()
            };

            await client.CreateDocument( "questions", document);
            
            Console.Write( $"Created question with id {message.QuestionId}" );
        }

        private static string StripHtml( string input )
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", String.Empty);
        }
    }
}
