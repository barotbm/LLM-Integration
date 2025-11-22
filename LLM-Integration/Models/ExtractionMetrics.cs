namespace LLM_Integration.Models;

/// <summary>
/// Represents observability metrics and statistics for an invoice extraction operation.
/// Tracks token usage, response times, and other performance indicators.
/// </summary>
public record ExtractionMetrics
{
    /// <summary>
    /// Total number of tokens used in the prompt (input).
    /// </summary>
    public int PromptTokens { get; init; }

    /// <summary>
    /// Total number of tokens generated in the completion (output).
    /// </summary>
    public int CompletionTokens { get; init; }

    /// <summary>
    /// Total number of tokens used (PromptTokens + CompletionTokens).
    /// </summary>
    public int TotalTokens { get; init; }

    /// <summary>
    /// Time taken to send the request to OpenAI API (milliseconds).
    /// </summary>
    public long RequestDurationMs { get; init; }

    /// <summary>
    /// Time taken by OpenAI to process and return the response (milliseconds).
    /// </summary>
    public long ProcessingDurationMs { get; init; }

    /// <summary>
    /// Total time from request start to response received (milliseconds).
    /// </summary>
    public long TotalDurationMs { get; init; }

    /// <summary>
    /// HTTP status code returned by the API.
    /// </summary>
    public int HttpStatusCode { get; init; }

    /// <summary>
    /// The model used for the extraction (e.g., "gpt-4o-2024-08-06").
    /// </summary>
    public string? Model { get; init; }

    /// <summary>
    /// Unique request ID from OpenAI API for tracing.
    /// </summary>
    public string? RequestId { get; init; }

    /// <summary>
    /// Timestamp when the extraction request was initiated (UTC).
    /// </summary>
    public DateTime RequestTimestampUtc { get; init; }

    /// <summary>
    /// Estimated cost of this extraction in USD.
    /// Based on OpenAI's current pricing for the model.
    /// </summary>
    public decimal EstimatedCostUsd { get; init; }

    /// <summary>
    /// Size of the input invoice text in bytes.
    /// </summary>
    public int InputSizeBytes { get; init; }

    /// <summary>
    /// Size of the extracted JSON response in bytes.
    /// </summary>
    public int ResponseSizeBytes { get; init; }

    /// <summary>
    /// Whether the extraction completed successfully.
    /// </summary>
    public bool IsSuccessful { get; init; }

    /// <summary>
    /// Error message if extraction failed, null if successful.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Finish reason from OpenAI (e.g., "stop", "length", "content_filter").
    /// </summary>
    public string? FinishReason { get; init; }

    /// <summary>
    /// Human-readable summary of the metrics.
    /// </summary>
    public override string ToString()
    {
        return $"""
            Extraction Metrics:
            - Tokens: {TotalTokens} (prompt: {PromptTokens}, completion: {CompletionTokens})
            - Duration: {TotalDurationMs}ms (network: {RequestDurationMs}ms, processing: {ProcessingDurationMs}ms)
            - Model: {Model}
            - Status: {HttpStatusCode}
            - Cost: ${EstimatedCostUsd:F6}
            - Input Size: {InputSizeBytes} bytes
            - Response Size: {ResponseSizeBytes} bytes
            - Request ID: {RequestId}
            - Timestamp: {RequestTimestampUtc:yyyy-MM-dd HH:mm:ss.fff} UTC
            - Finish Reason: {FinishReason}
            {(IsSuccessful ? "" : $"- Error: {ErrorMessage}")}
            """;
    }
}
