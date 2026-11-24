using Buteo.EdgeCollector.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Buteo.EdgeCollector.Api.Endpoints;

public static class HostEndpoints
{
    public static void MapHostEndpoints(this WebApplication app)
    {
        var hosts = app.MapGroup("/api/hosts");

        // Lista todos os servidores registados
        hosts.MapGet("", async (BancoDbContext db) =>
        {
            var list = await db.Hosts.ToListAsync();
            return Results.Ok(list);
        });

        // recebe o sinal de vida de um servidor e atualiza o último contacto
        hosts.MapPost("/{id:int}/ping", async (int id, BancoDbContext db) =>
        {
            var host = await db.Hosts.FindAsync(id);
            if (host is null)
                return Results.NotFound(new { error = $"Host {id} not found." });

            host.Timestamp = DateTime.UtcNow;
            host.HostStatus = true;

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                host.HostID,
                host.Hostname,
                host.HostStatus,
                lastPing = host.Timestamp
            });
        });
    }
}