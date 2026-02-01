using Typesense;

namespace SearchService.Data
{
    public static class SearchInitializer
    {
        public static async Task EnsureIndexExists( ITypesenseClient client)
        {
            const string schemaName = "questions";

            try
            {
                // I put this here in case I need to remove a collection and recreate it
                // await client.DeleteCollection(schemaName);

                await client.RetrieveCollection(schemaName);
                    
                Console.WriteLine($"Collection {schemaName} has been created.");
                return;
            }
            catch (TypesenseApiNotFoundException ex)
            {
                Console.WriteLine($"Collection {schemaName} has not been created yet.");
            }

            var schema = new Schema(schemaName, new List<Field>()
            {
                    new Field("id", FieldType.String),
                    new Field("title", FieldType.String),
                    new Field("content", FieldType.String),
                    new Field("tags", FieldType.StringArray),
                    new Field("createdAt", FieldType.Int64),
                    new Field("hasAcceptedAnswer", FieldType.Bool),
                    new Field("answerCount", FieldType.Int32)
            })
            {
                DefaultSortingField = "createdAt"
            };

            await client.CreateCollection(schema);

            Console.WriteLine($"Collection {schemaName} has been created.");
            return;
        }
    }
}
