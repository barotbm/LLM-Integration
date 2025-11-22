using LLM_Integration.Models;

namespace LLM_Integration.Services;

/// <summary>
/// Interface for parsing and extracting invoice data from raw text.
/// </summary>
public interface IInvoiceParser
{
    /// <summary>
    /// Extracts invoice data from raw invoice text with observability metrics.
    /// </summary>
    /// <param name="invoiceText">Raw text content of the invoice.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Extraction result containing data and metrics.</returns>
    Task<ExtractionResult> ExtractInvoiceAsync(string invoiceText, CancellationToken cancellationToken = default);
}

/// <summary>
/// Extension methods for invoice parsing.
/// </summary>
public static class InvoiceParserExtensions
{
    /// <summary>
    /// Extracts invoice data from raw invoice text without metrics (legacy/simple usage).
    /// </summary>
    /// <param name="parser">The invoice parser.</param>
    /// <param name="invoiceText">Raw text content of the invoice.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Extracted invoice data as an InvoiceExtractionResult. Null if extraction failed.</returns>
    public static async Task<InvoiceExtractionResult?> ExtractInvoiceSimpleAsync(
        this IInvoiceParser parser,
        string invoiceText,
        CancellationToken cancellationToken = default)
    {
        var result = await parser.ExtractInvoiceAsync(invoiceText, cancellationToken);
        return result.Data;
    }
}
