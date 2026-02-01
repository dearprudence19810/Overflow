using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var typesenseApiKey = builder.Configuration.GetValue<string>("typesense-api-key");

if (string.IsNullOrEmpty(typesenseApiKey))
{
    throw new Exception("typesense-api-key key not found in appsettings.json");
}

#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var keycloak = builder
    .AddKeycloak("keycloak", 6001)
    .WithoutHttpsCertificate()
    .WithDataVolume("keycloak-data");


var postgres = builder.AddPostgres("postgres", port: 5432)
    .WithDataVolume("postgres-data")
    // .WithPgAdmin();
    .WithPgAdmin(pgAdmin =>
    {
        pgAdmin.WithHostPort(5050);
    });

var typesense = builder.AddContainer("typesense", "typesense/typesense", "29.0")
    .WithArgs("--data-dir", "/data", "--api-key", "xyz", "--enable-cors")
    .WithVolume("typesense-data", "/data")
    .WithHttpEndpoint(8108, 8108, name: "typesense" );

var typesenseContainer = typesense.GetEndpoint("typesense");

var questionDb = postgres.AddDatabase("questionDb");

var rabbitMq = builder.AddRabbitMQ("messaging", port: 5672)
    .WithDataVolume("rabbitmq-data")
    .WithManagementPlugin(15672);

#pragma warning restore ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

builder.AddProject<Projects.QuestionService>("question-svc")
    .WithReference(keycloak)
    .WithReference(questionDb)
    .WithReference(rabbitMq)
    .WaitFor(keycloak)
    .WaitFor(questionDb)   
    .WaitFor(rabbitMq); 

builder.AddProject<Projects.SearchService>("search-svc")
    .WithEnvironment("typesense-api-key", typesenseApiKey )
    .WithReference(typesenseContainer)
    .WithReference(rabbitMq)
    .WaitFor(typesense)
    .WaitFor(rabbitMq);

builder.Build().Run();
