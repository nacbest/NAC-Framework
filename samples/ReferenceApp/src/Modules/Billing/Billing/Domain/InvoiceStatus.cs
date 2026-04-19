namespace Billing.Domain;

/// <summary>Lifecycle status of a billing invoice.</summary>
internal enum InvoiceStatus
{
    Pending   = 0,
    Paid      = 1,
    Cancelled = 2,
}
