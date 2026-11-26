using Buteo.EdgeCollector.Api.Data;
using Buteo.EdgeCollector.Api.Models;
using Buteo.EdgeCollector.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace Buteo.EdgeCollector.Api.Endpoints;

public static class HostEndpoints
{
    public static void MapHostEndpoints(this WebApplication app)
    {
        var hosts = app.MapGroup("/api/hosts");

        // Lista todos os servidores e verifica se algum ultrapassou o tempo limite sem responder
        hosts.MapGet("", async (BancoDbContext db) =>
        {
            List<Models.Host> allHosts = await db.Hosts.ToListAsync();

            DetectionRule? rule = await db.DetectionRules
                .FirstOrDefaultAsync(r => r.IsEnabled && r.EventType == EventTypes.HostInactivity);

            if (rule != null)
            {
                int timeoutMinutes = rule.TimeWindowMinutes ?? 30;

                foreach (Models.Host host in allHosts)
                {
                    // Calcula quanto tempo passou desde o último ping deste servidor
                    TimeSpan tempoSemResposta = DateTime.UtcNow - host.Timestamp;

                    if (host.HostStatus && tempoSemResposta.TotalMinutes > timeoutMinutes)
                    {
                        SecurityEvent securityEvent = new SecurityEvent
                        {
                            HostID = host.HostID,
                            EventType = EventTypes.HostInactivity,
                            RawPayload = "{\"hostname\":\"" + host.Hostname + "\"}",
                            IPAddress = host.IPAddress,
                            Timestamp = DateTime.UtcNow
                        };

                        db.SecurityEvents.Add(securityEvent);
                        await db.SaveChangesAsync();

                        Incident newIncident = new Incident
                        {
                            SecurityEventID = securityEvent.SecurityEventID,
                            DetectionRuleID = rule.DetectionRuleID,
                            Title = rule.RuleName,
                            IncidentDescription = "O servidor " + host.Hostname + " (" + host.IPAddress + ") não responde há mais de " + timeoutMinutes + " minutos.",
                            Severity = rule.Severity,
                            Status = "New",
                            Timestamp = DateTime.UtcNow
                        };

                        db.Incidents.Add(newIncident);

                        // Marca o servidor como offline para não criar incidentes repetidos
                        host.HostStatus = false;
                    }
                }

                await db.SaveChangesAsync();
            }

            return Results.Ok(allHosts);
        });

        // Recebe o sinal de vida (ping) de um servidor e volta a marcá-lo como ativo
        hosts.MapPost("/{id:int}/ping", async (int id, BancoDbContext db) =>
        {
            Models.Host? host = await db.Hosts.FindAsync(id);
            if (host == null)
            {
                return Results.NotFound(new { error = "Host " + id + " not found." });
            }

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