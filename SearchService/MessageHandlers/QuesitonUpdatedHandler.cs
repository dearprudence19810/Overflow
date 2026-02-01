using Contracts;
using SearchService.Models;
using Typesense;
using Wolverine.Runtime;
using Wolverine.Runtime.Handlers;

namespace SearchService.MessageHandlers
{
    public class QuesitonUpdatedHandler(ITypesenseClient client)
    {
        public async Task HandleAsync(QuestionUpdated message)
        {
            //var created = new DateTimeOffset(message.Created).ToUnixTimeSeconds();

            var document = new SearchQuestion()
            {
                Id = message.QuestionId,
                Title = message.Title,
                Content = StripHtml(message.Content),
                Tags = message.Tags.ToArray()
            };

            // update with an anonymous object so that don't overwrite the created date and other nullable fields from SearchQuestion

            await client.UpdateDocument("questions", message.QuestionId, new
            {
                Title = message.Title,
                Content = StripHtml(message.Content),
                Tags = message.Tags.ToArray()
            });

            Console.Write($"Updated question with id {message.QuestionId}");
        }

        private static string StripHtml(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", String.Empty);
        }
    }
}
