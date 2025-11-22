using LLM_Integration.Models;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace LLM_Integration.Services;

/// <summary>
/// Service for extracting invoice data using the OpenAI API with GPT-4o and Structured Outputs.
/// Includes comprehensive observability metrics including token usage, response times, and costs.
/// </summary>
public class OpenAIInvoiceService : IInvoiceParser
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string SystemPrompt = "You are a financial data extraction assistant. Extract data strictly. If a field is missing, return null.";
    private const string Model = "gpt-4o-2024-08-06";
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

    // Pricing per 1M tokens (as of November 2024)
    private const decimal InputTokenCostPer1M = 2.50m;      // $2.50 per 1M input tokens
    private const decimal OutputTokenCostPer1M = 10.00m;    // $10.00 per 1M output tokens

    public OpenAIInvoiceService(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
        }

        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Extracts invoice data from raw invoice text using GPT-4o with Structured Outputs.
    /// Includes comprehensive observability metrics for monitoring and cost tracking.
    /// </summary>
    /// <param name="invoiceText">Raw text content of the invoice.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Extraction result containing data and detailed metrics.</returns>
    public async Task<ExtractionResult> ExtractInvoiceAsync(string invoiceText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceText))
        {
            throw new ArgumentException("Invoice text cannot be null or empty.", nameof(invoiceText));
        }

        var overallStopwatch = Stopwatch.StartNew();
        var requestTimestampUtc = DateTime.UtcNow;
        var inputSizeBytes = Encoding.UTF8.GetByteCount(invoiceText);

        try
        {
            var userMessage = $"""
                Extract the following information from this invoice text and return ONLY valid JSON:
                - InvoiceNumber (string or null)
                - VendorName (string or null)
                - InvoiceDate (ISO 8601 format string or null)
                - TotalAmount (decimal number)
                - LineItems (array of objects with Description and Amount)

                Invoice text:
                {invoiceText}
                """;

            var requestBody = new
            {
                model = Model,
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = userMessage }
                },
                response_format = new { type = "json_object" },
                temperature = 0
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
            {
                Content = JsonContent.Create(requestBody)
            };

            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

            // Measure network request time
            var networkStopwatch = Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request, cancellationToken);
            networkStopwatch.Stop();

            var httpStatusCode = (int)response.StatusCode;

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                overallStopwatch.Stop();
                return CreateFailureResult(
                    requestTimestampUtc,
                    overallStopwatch.ElapsedMilliseconds,
                    networkStopwatch.ElapsedMilliseconds,
                    httpStatusCode,
                    inputSizeBytes,
                    ex.Message,
                    null);
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseSizeBytes = Encoding.UTF8.GetByteCount(jsonContent);

            var jsonDocument = JsonDocument.Parse(jsonContent);
            var root = jsonDocument.RootElement;

            // Extract metrics from response
            var usage = root.GetProperty("usage");
            var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
            var completionTokens = usage.GetProperty("completion_tokens").GetInt32();
            var totalTokens = usage.GetProperty("total_tokens").GetInt32();

            var finishReason = root.GetProperty("choices")[0]
                .GetProperty("finish_reason")
                .GetString() ?? "unknown";

            var responseContent = root.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            string? requestId = null;
            if (root.TryGetProperty("id", out var idElement))
            {
                requestId = idElement.GetString();
            }

            if (string.IsNullOrWhiteSpace(responseContent))
            {
                overallStopwatch.Stop();
                return CreateFailureResult(
                    requestTimestampUtc,
                    overallStopwatch.ElapsedMilliseconds,
                    networkStopwatch.ElapsedMilliseconds,
                    httpStatusCode,
                    inputSizeBytes,
                    "OpenAI API returned empty response.",
                    requestId);
            }

            var extractedData = JsonSerializer.Deserialize<InvoiceExtractionResult>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (extractedData == null)
            {
                overallStopwatch.Stop();
                return CreateFailureResult(
                    requestTimestampUtc,
                    overallStopwatch.ElapsedMilliseconds,
                    networkStopwatch.ElapsedMilliseconds,
                    httpStatusCode,
                    inputSizeBytes,
                    "Failed to deserialize API response to InvoiceExtractionResult.",
                    requestId);
            }

            overallStopwatch.Stop();

            // Calculate estimated cost
            var estimatedCost = CalculateEstimatedCost(promptTokens, completionTokens);

            // Calculate processing duration (total - network)
            var processingDuration = overallStopwatch.ElapsedMilliseconds - networkStopwatch.ElapsedMilliseconds;

            var metrics = new ExtractionMetrics
            {
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = totalTokens,
                RequestDurationMs = networkStopwatch.ElapsedMilliseconds,
                ProcessingDurationMs = processingDuration,
                TotalDurationMs = overallStopwatch.ElapsedMilliseconds,
                HttpStatusCode = httpStatusCode,
                Model = Model,
                RequestId = requestId,
                RequestTimestampUtc = requestTimestampUtc,
                EstimatedCostUsd = estimatedCost,
                InputSizeBytes = inputSizeBytes,
                ResponseSizeBytes = responseSizeBytes,
                IsSuccessful = true,
                ErrorMessage = null,
                FinishReason = finishReason
            };

            return new ExtractionResult
            {
                Data = extractedData,
                Metrics = metrics
            };
        }
        catch (OperationCanceledException ex)
        {
            overallStopwatch.Stop();
            return CreateFailureResult(
                requestTimestampUtc,
                overallStopwatch.ElapsedMilliseconds,
                0,
                0,
                inputSizeBytes,
                $"Operation cancelled: {ex.Message}",
                null);
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            return CreateFailureResult(
                requestTimestampUtc,
                overallStopwatch.ElapsedMilliseconds,
                0,
                0,
                inputSizeBytes,
                $"Unexpected error: {ex.Message}",
                null);
        }
    }

    /// <summary>
    /// Creates a failure result with appropriate error metrics.
    /// </summary>
    private static ExtractionResult CreateFailureResult(
        DateTime requestTimestampUtc,
        long totalDurationMs,
        long networkDurationMs,
        int httpStatusCode,
        int inputSizeBytes,
        string errorMessage,
        string? requestId)
    {
        var metrics = new ExtractionMetrics
        {
            PromptTokens = 0,
            CompletionTokens = 0,
            TotalTokens = 0,
            RequestDurationMs = networkDurationMs,
            ProcessingDurationMs = totalDurationMs - networkDurationMs,
            TotalDurationMs = totalDurationMs,
            HttpStatusCode = httpStatusCode,
            Model = Model,
            RequestId = requestId,
            RequestTimestampUtc = requestTimestampUtc,
            EstimatedCostUsd = 0,
            InputSizeBytes = inputSizeBytes,
            ResponseSizeBytes = 0,
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            FinishReason = "error"
        };

        return new ExtractionResult
        {
            Data = null,
            Metrics = metrics
        };
    }

    /// <summary>
    /// Calculates the estimated cost of the API call based on token usage.
    /// Uses current OpenAI pricing for GPT-4o model.
    /// </summary>
    private static decimal CalculateEstimatedCost(int promptTokens, int completionTokens)
    {
        var inputCost = (promptTokens / 1_000_000m) * InputTokenCostPer1M;
        var outputCost = (completionTokens / 1_000_000m) * OutputTokenCostPer1M;
        return inputCost + outputCost;
    }
}

