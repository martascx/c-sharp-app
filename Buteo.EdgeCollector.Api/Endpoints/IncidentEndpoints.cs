using Buteo.EdgeCollector.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Buteo.EdgeCollector.Api.Endpoints;

public static class IncidentEndpoints
{
    public static void MapIncidentEndpoints(this WebApplication app)
    {
        var incidents = app.MapGroup("/api/incidents");

        incidents.MapGet("", async (BancoDbContext db) =>
            Results.Ok(await db.Incidents.OrderByDescending(i => i.Timestamp).ToListAsync()));

        incidents.MapDelete("/{id:int}", async (int id, BancoDbContext db) =>
        {
            var incident = await db.Incidents.FindAsync(id);
            if (incident is null)
                return Results.NotFound(new { error = $"Incident {id} not found." });

            db.Incidents.Remove(incident);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
