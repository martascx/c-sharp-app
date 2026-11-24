using Buteo.EdgeCollector.Api.Data;
using Buteo.EdgeCollector.Api.Endpoints;
using Buteo.EdgeCollector.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Ligação do BancoDbContext ao SQL Server com uma Connection String
builder.Services.AddDbContext<BancoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<TokenStore>();
builder.Services.AddScoped<RuleEngineService>();

builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Endpoints dos eventos de segurança (issues #3, #4, #5)
app.MapSecurityEventEndpoints();
app.MapHostEndpoints();
// Login do admin (issue #1)
app.MapAuthEndpoints();

app.Run();
