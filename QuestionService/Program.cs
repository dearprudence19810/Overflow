using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using QuestionService.Data;
using QuestionService.Services;
using Wolverine;
using Wolverine.RabbitMQ;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.AddServiceDefaults();

builder.Services.AddMemoryCache();

builder.Services.AddScoped<TagService>();

builder.Services.AddAuthentication().AddKeycloakJwtBearer(serviceName: "keycloak", realm: "overflow", options =>
    {
        options.RequireHttpsMetadata = false;
        options.Audience = "overflow"; 
        // options.TokenValidationParameters.ValidateAudience = false;
        
    }
);

builder.AddNpgsqlDbContext<QuestionService.Data.QuestionDbContext>("questionDb");

builder.Services.AddOpenTelemetry().WithTracing(traceBuilderProvider  =>
{
    traceBuilderProvider.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService(builder.Environment.ApplicationName))
        .AddSource("Wolverine");        
});

var conn = builder.Configuration["Wolverine:RabbitMq:messaging:ConnectionString"];
Console.WriteLine(conn ?? "NOT FOUND");








builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMqUsingNamedConnection("messaging")
    //opts.UseRabbitMq("amqp://guest:guest@host:5672/")
    .AutoProvision();
    opts.PublishAllMessages().ToRabbitExchange("questions");

}); 


var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseAuthentication();
//app.UseAuthorization();

app.MapControllers();

app.MapDefaultEndpoints();

using var scope = app.Services.CreateScope();
var services = scope.ServiceProvider;

try
{
    var context = scope.ServiceProvider.GetRequiredService<QuestionDbContext>();
    await context.Database.MigrateAsync();
}
catch( Exception ex)
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while seeding the database.");
}

app.Run();
