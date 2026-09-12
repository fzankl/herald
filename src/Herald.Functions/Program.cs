using Herald.Functions.Extensions;
using Microsoft.Azure.Functions.Worker.Builder;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddHeraldOptions()
    .ConfigureHeraldJson();

builder.Build().Run();
