using Herald.Core;
using Herald.Functions.Extensions;
using Microsoft.Azure.Functions.Worker.Builder;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddHeraldCore()
    .AddHeraldOptions()
    .ConfigureHeraldJson();

builder.Build().Run();
