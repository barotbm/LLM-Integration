namespace LLM_Integration.Models;

/// <summary>
/// Represents the complete result of an invoice extraction operation,
/// including both the extracted data and observability metrics.
/// </summary>
public record ExtractionResult
{
    /// <summary>
    /// The extracted invoice data. Null if extraction failed.
    /// </summary>
    public InvoiceExtractionResult? Data { get; init; }

    /// <summary>
    /// Comprehensive metrics about the extraction operation.
    /// </summary>
    public required ExtractionMetrics Metrics { get; init; }

    /// <summary>
    /// Whether the extraction was successful.
    /// </summary>
    public bool Success => Data != null && Metrics.IsSuccessful;
}
