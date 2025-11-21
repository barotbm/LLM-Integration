namespace LLM_Integration.Models;

/// <summary>
/// Represents a line item in an invoice.
/// </summary>
public record LineItem
{
    /// <summary>
    /// Description of the line item.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Amount for the line item.
    /// </summary>
    public required decimal Amount { get; init; }
}
