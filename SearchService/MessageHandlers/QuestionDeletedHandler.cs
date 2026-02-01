using Contracts;
using SearchService.Models;
using Typesense;
using Wolverine.Runtime;
using Wolverine.Runtime.Handlers;

namespace SearchService.MessageHandlers
{
    public class QuestionDeletedHandler(ITypesenseClient client)
    {
        public async Task HandleAsync(QuestionDeleted message)
        {
            await client.DeleteDocument<SearchQuestion>("questions", message.QuestionId);

            Console.Write($"Deleted question with id {message.QuestionId}");
        }

        private static string StripHtml(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", String.Empty);
        }
    }
}
