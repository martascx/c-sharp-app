using System.Text.Json;
using Buteo.EdgeCollector.Api.Data;
using Buteo.EdgeCollector.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Buteo.EdgeCollector.Api.Services;

public class RuleEngineService
{
    private readonly BancoDbContext _db;

    public RuleEngineService(BancoDbContext db)
    {
        _db = db;
    }

    // procura regras ativas para o evento e abre incidentes se necessário
    public async Task EvaluateEventAsync(SecurityEvent securityEvent)
    {
        var activeRules = await _db.DetectionRules
            .Where(r => r.IsEnabled && r.EventType == securityEvent.EventType)
            .ToListAsync();

        foreach (var rule in activeRules)
        {
            var (triggered, details) = securityEvent.EventType switch
            {
                EventTypes.FailedLogin => await CheckFailedLoginAsync(securityEvent, rule),
                EventTypes.Transfer => await CheckLargeTransferAsync(securityEvent, rule),
                EventTypes.TransactionFrequency => await CheckTransactionBurstAsync(securityEvent, rule),
                EventTypes.ClientHourlyLimit => await CheckClientHourlyLimitAsync(securityEvent, rule),
                _ => (false, string.Empty)
            };

            if (triggered)
            {
                _db.Incidents.Add(new Incident
                {
                    SecurityEventID = securityEvent.SecurityEventID,
                    DetectionRuleID = rule.DetectionRuleID,
                    Title = rule.RuleName,
                    IncidentDescription = details,
                    Severity = rule.Severity,
                    Status = "New",
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        await _db.SaveChangesAsync();
    }

    // regra 1: conta tentativas de login falhadas dentro da janela temporal 
    private async Task<(bool Triggered, string Details)> CheckFailedLoginAsync(SecurityEvent ev, DetectionRule rule)
    {
        int windowMinutes = rule.TimeWindowMinutes ?? 10;
        decimal maxAttempts = rule.Threshold ?? 5;
        var windowStart = DateTime.UtcNow.AddMinutes(-windowMinutes);

        int failedAttempts = await _db.SecurityEvents.CountAsync(e =>
            e.EventType == EventTypes.FailedLogin &&
            e.IPAddress == ev.IPAddress &&
            e.Timestamp >= windowStart);

        bool triggered = failedAttempts >= maxAttempts;
        string details = $"{failedAttempts} tentativas de login falhadas a partir do IP {ev.IPAddress} nos últimos {windowMinutes} minutos.";

        return (triggered, details);
    }

    // regra 2: verifica se o montante da transferência ultrapassa o valor que queremos 
    private async Task<(bool Triggered, string Details)> CheckLargeTransferAsync(SecurityEvent ev, DetectionRule rule)
    {
        decimal maxAmount = rule.Threshold ?? 10000m;
        decimal amount = await ResolveTransactionAmountAsync(ev);

        bool triggered = amount > maxAmount;
        string details = $"Transferência de {amount:N2} € excedeu o limite de {maxAmount:N2} € (requer justificação).";

        return (triggered, details);
    }

    // regra 3: deteta rajadas de transações vindas do mesmo IP em um determinado espaço de tempo
    private async Task<(bool Triggered, string Details)> CheckTransactionBurstAsync(SecurityEvent ev, DetectionRule rule)
    {
        int windowMinutes = rule.TimeWindowMinutes ?? 1;
        decimal maxEvents = rule.Threshold ?? 5;
        var windowStart = DateTime.UtcNow.AddMinutes(-windowMinutes);

        int recentEvents = await _db.SecurityEvents.CountAsync(e =>
            (e.EventType == EventTypes.Transfer || e.EventType == EventTypes.TransactionFrequency) &&
            e.IPAddress == ev.IPAddress &&
            e.Timestamp >= windowStart);

        bool triggered = recentEvents >= maxEvents;
        string details = $"{recentEvents} pedidos de transação detetados do IP {ev.IPAddress} em menos de {windowMinutes} minuto(s).";

        return (triggered, details);
    }

    // regra 4: verifica se o mesmo cliente fez demasiadas transações na ultima hora
    private async Task<(bool Triggered, string Details)> CheckClientHourlyLimitAsync(SecurityEvent ev, DetectionRule rule)
    {
        if (ev.TransactionID is not int transactionId)
            return (false, string.Empty);

        var transaction = await _db.BankTransactions.FindAsync(transactionId);
        if (transaction is null)
            return (false, string.Empty);

        int windowMinutes = rule.TimeWindowMinutes ?? 60;
        decimal maxTransactions = rule.Threshold ?? 10;
        var windowStart = DateTime.UtcNow.AddMinutes(-windowMinutes);

        int clientTransactions = await _db.BankTransactions.CountAsync(t =>
            t.SenderIBAN == transaction.SenderIBAN &&
            t.Timestamp >= windowStart);

        bool triggered = clientTransactions >= maxTransactions;
        string details = $"O cliente {transaction.SenderIBAN} realizou {clientTransactions} transações nos últimos {windowMinutes} minutos.";

        return (triggered, details);
    }

    // obtém o valor da transação na BD ou em alternativa lê o campo "amount" do JSON
    private async Task<decimal> ResolveTransactionAmountAsync(SecurityEvent ev)
    {
        if (ev.TransactionID is int transactionId)
        {
            var transaction = await _db.BankTransactions.FindAsync(transactionId);
            if (transaction is not null)
                return transaction.Amount;
        }

        try
        {
            using var json = JsonDocument.Parse(ev.RawPayload);
            if (json.RootElement.TryGetProperty("amount", out var amountProp) &&
                amountProp.TryGetDecimal(out var parsedAmount))
            {
                return parsedAmount;
            }
        }
        catch (JsonException)
        {
            
        }

        return 0m;
    }
}