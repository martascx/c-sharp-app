using Buteo.EdgeCollector.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Ligação do BancoDbContext ao SQL Server com uma Connection String
builder.Services.AddDbContext<BancoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.Run();
