namespace Billing.Contracts.DTOs;

/// <summary>Public read model returned by GET /api/invoices/{id}.</summary>
public sealed record InvoiceResponse(
    Guid Id,
    Guid OrderId,
    decimal Amount,
    string Status,
    DateTime CreatedAt);
