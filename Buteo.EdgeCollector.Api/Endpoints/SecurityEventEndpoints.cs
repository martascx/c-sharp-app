using System.Net;
using Buteo.EdgeCollector.Api.Data;
using Buteo.EdgeCollector.Api.Dtos;
using Buteo.EdgeCollector.Api.Models;
using Buteo.EdgeCollector.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Buteo.EdgeCollector.Api.Endpoints;

public static class SecurityEventEndpoints
{
    public static void MapSecurityEventEndpoints(this WebApplication app)
    {
        var events = app.MapGroup("");

        // #3 - GET /api/events -> gets all events
        events.MapGet("/api/events", async (BancoDbContext db) =>
        {
            var list = await db.SecurityEvents
                .OrderByDescending(e => e.Timestamp)
                .ToListAsync();

            return Results.Ok(list);
        });

        // #5 - GET /api/events/id -> gets event based on id
        events.MapGet("/api/events/{id:int}", async (int id, BancoDbContext db) =>
        {
            var securityEvent = await db.SecurityEvents.FindAsync(id);

            if (securityEvent is null)
            {
                return Results.NotFound(new { error = $"Event {id} not found." });
            }
            return Results.Ok(securityEvent);
        });

        // #4 - POST /api/events -> registers a suspicious event for the admin to review
        events.MapPost("/api/events", async (CreateSecurityEventRequest request, BancoDbContext db, RuleEngineService ruleEngine) =>
        {
            // 400 Bad Request: the data itself is invalid
            if (string.IsNullOrWhiteSpace(request.EventType) || request.EventType.Length > 50)
                return Results.BadRequest(new { error = "EventType is required." });

            if (string.IsNullOrWhiteSpace(request.RawPayload))
                return Results.BadRequest(new { error = "RawPayload is required." });

            if (!IPAddress.TryParse(request.IPAddress, out _))
                return Results.BadRequest(new { error = "IPAddress is not a valid IP address." });

            // 404 Not Found: the data refers to a host or transaction that doesn't exist
            if (!await db.Hosts.AnyAsync(h => h.HostID == request.HostID))
                return Results.NotFound(new { error = $"Host {request.HostID} not found." });

            if (request.TransactionID is int transactionId &&
                !await db.BankTransactions.AnyAsync(t => t.TransactionID == transactionId))
                return Results.NotFound(new { error = $"Transaction {transactionId} not found." });

            var securityEvent = new SecurityEvent
            {
                HostID = request.HostID,
                TransactionID = request.TransactionID,
                EventType = request.EventType.Trim().ToUpper(),
                RawPayload = request.RawPayload,
                IPAddress = request.IPAddress.Trim(),
                Timestamp = DateTime.UtcNow
            };

            db.SecurityEvents.Add(securityEvent);
            await db.SaveChangesAsync();

            // avalia as regras ativas contra o evento acabado de gravar
            await ruleEngine.EvaluateEventAsync(securityEvent);

            // 201 Created + Location header pointing to GET /api/events/{id}
            return Results.Created($"/api/events/{securityEvent.SecurityEventID}", securityEvent);
        });

        // #6 - DELETE /api/events/id -> deletes event based on id
        events.MapDelete("/api/events/{id:int}", async (int id, BancoDbContext db) =>
        {
            var securityEvent = await db.SecurityEvents.FindAsync(id);

            if (securityEvent is null)
                return Results.NotFound(new { error = $"Event {id} not found." });

            if (await db.Incidents.AnyAsync(i => i.SecurityEventID == id))
                return Results.Conflict(new { error = $"Event {id} has incidents and cannot be deleted." });

            db.SecurityEvents.Remove(securityEvent);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }
}
