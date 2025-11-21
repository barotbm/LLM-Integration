namespace LLM_Integration.Models;

/// <summary>
/// Represents the result of invoice extraction from raw text.
/// </summary>
public record InvoiceExtractionResult
{
    /// <summary>
    /// The invoice number extracted from the invoice text.
    /// </summary>
    public string? InvoiceNumber { get; init; }

    /// <summary>
    /// The vendor name extracted from the invoice text.
    /// </summary>
    public string? VendorName { get; init; }

    /// <summary>
    /// The invoice date extracted from the invoice text. May be null if not found.
    /// </summary>
    public DateTime? InvoiceDate { get; init; }

    /// <summary>
    /// The total amount of the invoice.
    /// </summary>
    public decimal TotalAmount { get; init; }

    /// <summary>
    /// Collection of line items included in the invoice.
    /// </summary>
    public List<LineItem> LineItems { get; init; } = new();
}
