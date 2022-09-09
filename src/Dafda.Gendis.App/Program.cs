using Dafda.Gendis.App;
using Dafda.Gendis.App.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.ConfigureSerilog();
builder.ConfigureDatabase();
builder.ConfigureKafkaProducer();

builder.Services.AddTransient<QueueProcessor>();

builder.Services.AddHostedService<Dispatcher>();

var app = builder.Build();



app.Run();
