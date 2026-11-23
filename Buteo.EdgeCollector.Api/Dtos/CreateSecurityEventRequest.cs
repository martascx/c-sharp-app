namespace Buteo.EdgeCollector.Api.Dtos;

public record CreateSecurityEventRequest(
    int HostID,
    int? TransactionID,
    string EventType,
    string RawPayload,
    string IPAddress);
