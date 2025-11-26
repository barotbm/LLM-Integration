namespace LLM_Integration.Models;

/// <summary>
/// Generic wrapper that combines extracted data with observability metrics.
/// </summary>
/// <typeparam name="T">The type of data being extracted.</typeparam>
public record ExtractionResult<T> where T : class
{
    /// <summary>
    /// The extracted data. Null if extraction failed.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Comprehensive metrics about the extraction operation.
    /// </summary>
    public required ExtractionMetrics Metrics { get; init; }

    /// <summary>
    /// Indicates whether the extraction was successful.
    /// </summary>
    public bool Success => Data != null && Metrics.IsSuccessful;
}

/// <summary>
/// Non-generic wrapper for backward compatibility with invoice extraction.
/// </summary>
public record ExtractionResult : ExtractionResult<InvoiceExtractionResult>
{
}
