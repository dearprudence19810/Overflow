using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using RabbitMQ.Client;
using SearchService.Data;
using SearchService.Models;
using System.Text.RegularExpressions;
using Typesense;
using Typesense.Setup;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry().WithTracing(traceBuilderProvider =>
{
    traceBuilderProvider.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(builder.Environment.ApplicationName))
        .AddSource("Wolverine");   
});

var conn = builder.Configuration["Wolverine:RabbitMq:messaging:ConnectionString"];
Console.WriteLine(conn ?? "NOT FOUND");

builder.Host.UseWolverine(opts =>
{
    //opts.UseRabbitMq("amqp://guest:guest@host:5672/")
    opts.UseRabbitMqUsingNamedConnection("messaging")
    .AutoProvision();
    opts.ListenToRabbitQueue("questions.search", cfg =>
    {
        cfg.BindExchange("questions");
    });
});

var typesenseUri = builder.Configuration["services:typesense:typesense:0"];

if (string.IsNullOrEmpty(typesenseUri))
{
    throw new Exception("Typesense URI is not configured");
}

var uri = new Uri(typesenseUri);

var typesenseApiKey = builder.Configuration.GetValue<string>("typesense-api-key");

if (string.IsNullOrEmpty(typesenseApiKey))
{
    throw new Exception("typesense-api-key key not found in appsettings.json");
}

builder.Services.AddTypesenseClient(config =>
{
    config.ApiKey = typesenseApiKey;
    config.Nodes = new List<Node>
    {
       new( uri.Host, uri.Port.ToString(), uri.Scheme )
    };

    //options.ConnectionTimeoutSeconds = 10;

});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();


app.MapGet("/search", async ( string query, ITypesenseClient client ) =>
{
    string? tag = null;

    var tagMatch = Regex.Match(query, @"\[(.*?)\]");

    if( tagMatch.Success)   
    {
        tag = tagMatch.Groups[1].Value;
        query = query.Replace(tagMatch.Value, "").Trim();
    }

    var searchParameters = new SearchParameters(query,"title,content");

    if( !string.IsNullOrEmpty(tag))
    {
        searchParameters.FilterBy = $"tags:=[{tag}]";
    }
    
    try
    {
        var results = await client.Search<SearchQuestion>( "questions", searchParameters);

        return Results.Ok(results.Hits.Select( h => h.Document ));
    }
    catch (Exception ex)
    {
        return Results.Problem( "Tyepesense search failed" , ex.Message );
    }
});


using var scope = app.Services.CreateScope();

var client = scope.ServiceProvider.GetRequiredService<ITypesenseClient>();

await SearchInitializer.EnsureIndexExists(client);  



app.Run();

