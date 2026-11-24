using Buteo.EdgeCollector.Api.Data;
using Buteo.EdgeCollector.Api.Endpoints;
using Buteo.EdgeCollector.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Ligação do BancoDbContext ao SQL Server com uma Connection String
builder.Services.AddDbContext<BancoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<TokenStore>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Endpoint de teste dos Hosts do SQL Server
app.MapGet("/api/hosts", async (BancoDbContext db) =>
{
    var hosts = await db.Hosts.ToListAsync();
    return Results.Ok(hosts);
});

// Endpoints dos eventos de segurança (issues #3, #4, #5)
app.MapSecurityEventEndpoints();

// Login do admin (issue #1)
app.MapAuthEndpoints();

app.Run();
