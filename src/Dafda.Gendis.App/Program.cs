using Dafda.Gendis.App;
using Dafda.Gendis.App.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<Dispatcher>();

builder.ConfigureSerilog();

var app = builder.Build();



app.Run();
