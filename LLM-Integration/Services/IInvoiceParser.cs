using LLM_Integration.Models;

namespace LLM_Integration.Services;

/// <summary>
/// Interface for parsing and extracting invoice data from raw text.
/// </summary>
public interface IInvoiceParser
{
    /// <summary>
    /// Extracts invoice data from raw invoice text.
    /// </summary>
    /// <param name="invoiceText">Raw text content of the invoice.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Extracted invoice data as an InvoiceExtractionResult.</returns>
    Task<InvoiceExtractionResult> ExtractInvoiceAsync(string invoiceText, CancellationToken cancellationToken = default);
}
