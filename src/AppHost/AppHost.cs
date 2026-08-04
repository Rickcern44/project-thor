var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

// Database resource named "Default" so the injected connection string key
// (ConnectionStrings__Default) matches docker-compose.yml and the K8s Secret key.
var db = postgres.AddDatabase("Default", "projectthor");

var api = builder.AddProject<Projects.ProjectThor_Api>("api")
    .WithReference(db)
    .WaitFor(db);

var web = builder.AddNpmApp("web", "../web", "dev")
    .WithReference(api)
    .WithHttpEndpoint(port: 5173, targetPort: 5173, isProxied: false)
    .WithExternalHttpEndpoints();

builder.Build().Run();
