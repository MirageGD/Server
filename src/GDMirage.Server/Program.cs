using GDMirage.Server.Configuration;
using GDMirage.Server.Features.Accounts;
using GDMirage.Server.Features.Content;
using GDMirage.Server.Features.Game;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog(options => options.ReadFrom
    .Configuration(builder.Configuration));

builder.Services.AddOptions<ServerOptions>()
    .Bind(builder.Configuration
        .GetSection("Server"))
    .ValidateDataAnnotations();

builder.Services.AddAccountServices(builder.Configuration);
builder.Services.AddGameServices();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        policy => policy
            .WithOrigins("http://localhost:8000")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors(policy =>
    policy.AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());

app.UseWebSockets();

app.UseAuthentication();
app.UseAuthorization();

app.MapContentEndpoint();
app.MapAccountEndpoints();
app.MapGameEndpoint();

app.Run();
