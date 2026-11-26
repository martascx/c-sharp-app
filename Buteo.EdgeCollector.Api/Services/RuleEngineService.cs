using System.Text.Json;
using Buteo.EdgeCollector.Api.Data;
using Buteo.EdgeCollector.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Buteo.EdgeCollector.Api.Services;

public class RuleEngineService
{
    // Atributo privado para aceder à base de dados (o "_" é convenção para atributos privados)
    private readonly BancoDbContext _db;

    // Construtor: recebe a ligação à base de dados quando o serviço é criado
    public RuleEngineService(BancoDbContext db)
    {
        _db = db;
    }

    // Método principal (public para ser chamado pelos endpoints): avalia se um evento viola alguma regra
    public async Task EvaluateEventAsync(SecurityEvent securityEvent)
    {
        // Vai buscar à BD apenas as regras ativas (IsEnabled) que correspondem ao tipo deste evento
        List<DetectionRule> activeRules = await _db.DetectionRules
            .Where(r => r.IsEnabled && r.EventType == securityEvent.EventType)
            .ToListAsync();

        // Percorre cada regra encontrada
        foreach (DetectionRule rule in activeRules)
        {
            // Guarda a mensagem do alerta. Se continuar vazia no fim, a regra não disparou
            string incidentDetails = "";

            // Encaminha para o método da regra certa consoante o tipo de evento
            switch (securityEvent.EventType)
            {
                case EventTypes.FailedLogin:
                    incidentDetails = await CheckFailedLoginAsync(securityEvent, rule);
                    break;

                case EventTypes.Transfer:
                    incidentDetails = await CheckLargeTransferAsync(securityEvent, rule);
                    break;

                case EventTypes.TransactionFrequency:
                    incidentDetails = await CheckTransactionBurstAsync(securityEvent, rule);
                    break;

                case EventTypes.ClientHourlyLimit:
                    incidentDetails = await CheckClientHourlyLimitAsync(securityEvent, rule);
                    break;
            }

            // Se o método devolveu texto, a regra foi violada e criamos um incidente na BD
            if (incidentDetails != "")
            {
                Incident newIncident = new Incident
                {
                    SecurityEventID = securityEvent.SecurityEventID,
                    DetectionRuleID = rule.DetectionRuleID,
                    Title = rule.RuleName,
                    IncidentDescription = incidentDetails,
                    Severity = rule.Severity,
                    Status = "New",
                    Timestamp = DateTime.UtcNow
                };

                _db.Incidents.Add(newIncident);
            }
        }

        // Grava todos os incidentes criados na base de dados de uma só vez
        await _db.SaveChangesAsync();
    }

    // Regra 1 (private porque só é usada dentro desta classe): verifica tentativas de login falhadas do mesmo IP
    private async Task<string> CheckFailedLoginAsync(SecurityEvent ev, DetectionRule rule)
    {
        // Lê os limites da BD ou usa valores por defeito caso estejam a null
        int windowMinutes = rule.TimeWindowMinutes ?? 10;
        decimal maxAttempts = rule.Threshold ?? 5;
        DateTime windowStart = DateTime.UtcNow.AddMinutes(-windowMinutes);

        // Conta quantos logins falhados este IP teve dentro da janela de tempo
        int failedAttempts = await _db.SecurityEvents.CountAsync(e =>
            e.EventType == EventTypes.FailedLogin &&
            e.IPAddress == ev.IPAddress &&
            e.Timestamp >= windowStart);

        if (failedAttempts >= maxAttempts)
        {
            return failedAttempts + " tentativas de login falhadas do IP " + ev.IPAddress + " nos últimos " + windowMinutes + " minutos.";
        }

        return "";
    }

    // Regra 2: verifica se o valor da transferência ultrapassa o limite máximo permitido
    private async Task<string> CheckLargeTransferAsync(SecurityEvent ev, DetectionRule rule)
    {
        decimal maxAmount = rule.Threshold ?? 10000m;
        decimal amount = await GetTransactionAmountAsync(ev);

        if (amount > maxAmount)
        {
            return "Transferência de " + amount.ToString("N2") + " € excedeu o limite de " + maxAmount.ToString("N2") + " €.";
        }

        return "";
    }

    // Regra 3: deteta várias transações seguidas vindas do mesmo IP num curto espaço de tempo
    private async Task<string> CheckTransactionBurstAsync(SecurityEvent ev, DetectionRule rule)
    {
        int windowMinutes = rule.TimeWindowMinutes ?? 1;
        decimal maxEvents = rule.Threshold ?? 5;
        DateTime windowStart = DateTime.UtcNow.AddMinutes(-windowMinutes);

        // Conta eventos de transferência ou frequência do mesmo IP no último minuto
        int recentEvents = await _db.SecurityEvents.CountAsync(e =>
            (e.EventType == EventTypes.Transfer || e.EventType == EventTypes.TransactionFrequency) &&
            e.IPAddress == ev.IPAddress &&
            e.Timestamp >= windowStart);

        if (recentEvents >= maxEvents)
        {
            return recentEvents + " pedidos de transação detetados do IP " + ev.IPAddress + " em " + windowMinutes + " minuto(s).";
        }

        return "";
    }

    // Regra 4: verifica se o mesmo cliente (IBAN) fez demasiadas transações na última hora
    private async Task<string> CheckClientHourlyLimitAsync(SecurityEvent ev, DetectionRule rule)
    {
        // Se o evento não tiver uma transação associada, não dá para saber o cliente
        if (ev.TransactionID == null)
        {
            return "";
        }

        // Procura a transação na BD para descobrir o IBAN de origem
        BankTransaction? transaction = await _db.BankTransactions.FindAsync(ev.TransactionID.Value);
        if (transaction == null)
        {
            return "";
        }

        int windowMinutes = rule.TimeWindowMinutes ?? 60;
        decimal maxTransactions = rule.Threshold ?? 10;
        DateTime windowStart = DateTime.UtcNow.AddMinutes(-windowMinutes);

        // Conta quantas transações esse IBAN fez nos últimos 60 minutos
        int clientTransactions = await _db.BankTransactions.CountAsync(t =>
            t.SenderIBAN == transaction.SenderIBAN &&
            t.Timestamp >= windowStart);

        if (clientTransactions >= maxTransactions)
        {
            return "O cliente " + transaction.SenderIBAN + " realizou " + clientTransactions + " transações nos últimos " + windowMinutes + " minutos.";
        }

        return "";
    }

    // Método auxiliar: obtém o valor da transação a partir da tabela BankTransaction ou do texto JSON do evento
    private async Task<decimal> GetTransactionAmountAsync(SecurityEvent ev)
    {
        // Primeiro tenta ir buscar o valor real à tabela das transações bancárias
        if (ev.TransactionID != null)
        {
            BankTransaction? transaction = await _db.BankTransactions.FindAsync(ev.TransactionID.Value);
            if (transaction != null)
            {
                return transaction.Amount;
            }
        }

        // Se ainda não houver registo na tabela (ex: testes via curl), lê o campo "amount" do JSON
        try
        {
            using JsonDocument json = JsonDocument.Parse(ev.RawPayload);
            if (json.RootElement.TryGetProperty("amount", out JsonElement amountProp))
            {
                return amountProp.GetDecimal();
            }
        }
        catch
        {
            // Se o texto não for JSON válido ou não tiver "amount", devolve 0 sem dar erro
        }

        return 0m;
    }
}