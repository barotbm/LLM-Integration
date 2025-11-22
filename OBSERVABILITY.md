# Observability & Metrics Documentation

## Overview

The Invoice Extraction Service now includes comprehensive observability metrics tracking token usage, response times, costs, and other operational statistics. All operations return an `ExtractionResult` wrapper containing both the extracted data and detailed metrics.

## Models

### ExtractionMetrics Record

Captures detailed information about an extraction operation:

```csharp
public record ExtractionMetrics
{
    public int PromptTokens { get; init; }              // Input tokens used
    public int CompletionTokens { get; init; }          // Output tokens generated
    public int TotalTokens { get; init; }               // Total tokens (input + output)
    
    public long RequestDurationMs { get; init; }        // Network round-trip time
    public long ProcessingDurationMs { get; init; }     // API processing time
    public long TotalDurationMs { get; init; }          // Total operation time
    
    public int HttpStatusCode { get; init; }            // HTTP response status
    public string? Model { get; init; }                 // Model used (e.g., "gpt-4o-2024-08-06")
    public string? RequestId { get; init; }             // OpenAI request ID for tracing
    
    public DateTime RequestTimestampUtc { get; init; }  // When request was sent
    public decimal EstimatedCostUsd { get; init; }      // Estimated API cost
    
    public int InputSizeBytes { get; init; }            // Raw input size
    public int ResponseSizeBytes { get; init; }         // API response size
    
    public bool IsSuccessful { get; init; }             // Success flag
    public string? ErrorMessage { get; init; }          // Error details if failed
    public string? FinishReason { get; init; }          // Why response ended (stop, length, etc.)
}
```

### ExtractionResult Record

Wraps extraction data with metrics:

```csharp
public record ExtractionResult
{
    public InvoiceExtractionResult? Data { get; init; }     // Extracted invoice data
    public required ExtractionMetrics Metrics { get; init; } // Operation metrics
    
    public bool Success => Data != null && Metrics.IsSuccessful;  // Convenience property
}
```

## Usage Examples

### Basic Extraction with Metrics

```csharp
var service = new OpenAIInvoiceService(apiKey);
var result = await service.ExtractInvoiceAsync(invoiceText);

if (result.Success)
{
    Console.WriteLine($"Invoice: {result.Data!.InvoiceNumber}");
    Console.WriteLine($"Vendor: {result.Data.VendorName}");
}

// Access metrics
Console.WriteLine($"Tokens Used: {result.Metrics.TotalTokens}");
Console.WriteLine($"Response Time: {result.Metrics.TotalDurationMs}ms");
Console.WriteLine($"Cost: ${result.Metrics.EstimatedCostUsd:F6}");
```

### Analyzing Performance

```csharp
var result = await service.ExtractInvoiceAsync(invoiceText);

// Timing breakdown
Console.WriteLine($"Network Time:    {result.Metrics.RequestDurationMs}ms");
Console.WriteLine($"Processing Time: {result.Metrics.ProcessingDurationMs}ms");
Console.WriteLine($"Total Time:      {result.Metrics.TotalDurationMs}ms");

// Token efficiency
var inputEfficiency = result.Metrics.InputSizeBytes / (double)result.Metrics.PromptTokens;
Console.WriteLine($"Bytes per Input Token: {inputEfficiency:F2}");
```

### Cost Tracking

```csharp
var result = await service.ExtractInvoiceAsync(invoiceText);

// Single extraction cost
Console.WriteLine($"Estimated Cost: ${result.Metrics.EstimatedCostUsd:F8}");

// Batch cost calculation
decimal totalCost = results.Sum(r => r.Metrics.EstimatedCostUsd);
int totalTokens = results.Sum(r => r.Metrics.TotalTokens);
Console.WriteLine($"Batch Cost: ${totalCost:F6}");
Console.WriteLine($"Average Cost per Invoice: ${totalCost / results.Count:F8}");
```

### Error Handling with Metrics

```csharp
var result = await service.ExtractInvoiceAsync(invoiceText);

if (!result.Success)
{
    Console.WriteLine($"Extraction failed: {result.Metrics.ErrorMessage}");
    Console.WriteLine($"HTTP Status: {result.Metrics.HttpStatusCode}");
    Console.WriteLine($"Time spent: {result.Metrics.TotalDurationMs}ms");
    // Log for debugging
    Console.WriteLine($"Request ID: {result.Metrics.RequestId}");
}
```

### Monitoring and Logging

```csharp
public class ExtractionLogger
{
    public void LogMetrics(ExtractionResult result)
    {
        var log = new
        {
            Timestamp = result.Metrics.RequestTimestampUtc,
            RequestId = result.Metrics.RequestId,
            Success = result.Metrics.IsSuccessful,
            Tokens = new
            {
                Prompt = result.Metrics.PromptTokens,
                Completion = result.Metrics.CompletionTokens,
                Total = result.Metrics.TotalTokens
            },
            Duration = new
            {
                Network = result.Metrics.RequestDurationMs,
                Processing = result.Metrics.ProcessingDurationMs,
                Total = result.Metrics.TotalDurationMs
            },
            Cost = result.Metrics.EstimatedCostUsd,
            FinishReason = result.Metrics.FinishReason,
            Error = result.Metrics.ErrorMessage
        };
        
        _logger.LogInformation("Extraction metrics: {@Log}", log);
    }
}
```

## Metrics Details

### Token Usage

- **PromptTokens**: Input tokens, includes system prompt + user message + invoice text
- **CompletionTokens**: Output tokens generated by the model
- **TotalTokens**: Sum of prompt and completion tokens
- Used for cost calculation and performance analysis

### Timing Metrics

| Metric | Description | Use Case |
|--------|-------------|----------|
| RequestDurationMs | Network latency (RTT) | Network performance monitoring |
| ProcessingDurationMs | API server processing time | Model performance analysis |
| TotalDurationMs | End-to-end operation time | SLA tracking, user experience |

### Cost Calculation

**Pricing (as of November 2024 for GPT-4o):**
- Input: $2.50 per 1M tokens
- Output: $10.00 per 1M tokens

**Formula:**
```
EstimatedCostUsd = (PromptTokens / 1,000,000) * 2.50 
                 + (CompletionTokens / 1,000,000) * 10.00
```

**Example:**
- 800 prompt tokens, 200 completion tokens
- Cost = (800 / 1,000,000) * 2.50 + (200 / 1,000,000) * 10.00
- Cost = $0.002 + $0.002 = $0.004

### Finish Reasons

| Reason | Meaning |
|--------|---------|
| "stop" | Model completed response normally |
| "length" | Response hit token limit |
| "content_filter" | OpenAI safety filters triggered |
| "error" | Local error occurred |

## Observability Best Practices

### 1. Structured Logging

```csharp
public static void LogExtractionMetrics(ExtractionResult result)
{
    if (!result.Success)
    {
        _logger.LogError(
            "Extraction failed - RequestId: {RequestId}, Status: {Status}, Error: {Error}",
            result.Metrics.RequestId,
            result.Metrics.HttpStatusCode,
            result.Metrics.ErrorMessage);
    }
    else
    {
        _logger.LogInformation(
            "Extraction succeeded - RequestId: {RequestId}, Tokens: {Tokens}, " +
            "Duration: {Duration}ms, Cost: ${Cost:F6}",
            result.Metrics.RequestId,
            result.Metrics.TotalTokens,
            result.Metrics.TotalDurationMs,
            result.Metrics.EstimatedCostUsd);
    }
}
```

### 2. Cost Alerting

```csharp
if (result.Metrics.EstimatedCostUsd > 0.10m)
{
    _alerting.SendAlert($"High cost extraction: ${result.Metrics.EstimatedCostUsd:F6}");
}
```

### 3. Performance Monitoring

```csharp
if (result.Metrics.TotalDurationMs > 5000)
{
    _logger.LogWarning(
        "Slow extraction detected: {Duration}ms (threshold: 5000ms)",
        result.Metrics.TotalDurationMs);
}
```

### 4. Token Efficiency Tracking

```csharp
var tokenEfficiency = invoiceText.Length / (double)result.Metrics.PromptTokens;
_metrics.RecordGauge("invoice_extraction.input_efficiency", tokenEfficiency);
_metrics.RecordGauge("invoice_extraction.tokens_used", result.Metrics.TotalTokens);
```

### 5. Batch Analytics

```csharp
public class ExtractionAnalytics
{
    public async Task<ExtractionStats> AnalyzeBatch(List<string> invoices)
    {
        var results = new List<ExtractionResult>();
        foreach (var invoice in invoices)
        {
            results.Add(await _service.ExtractInvoiceAsync(invoice));
        }
        
        return new ExtractionStats
        {
            TotalExtractions = results.Count,
            SuccessCount = results.Count(r => r.Success),
            FailureCount = results.Count(r => !r.Success),
            TotalTokensUsed = results.Sum(r => r.Metrics.TotalTokens),
            AverageTokensPerInvoice = results.Average(r => r.Metrics.TotalTokens),
            TotalCost = results.Sum(r => r.Metrics.EstimatedCostUsd),
            AverageCostPerInvoice = results.Average(r => r.Metrics.EstimatedCostUsd),
            MedianResponseTimeMs = results
                .OrderBy(r => r.Metrics.TotalDurationMs)
                .Skip(results.Count / 2)
                .First()
                .Metrics.TotalDurationMs,
            TotalResponseTimeMs = results.Sum(r => r.Metrics.TotalDurationMs)
        };
    }
}
```

## Metrics Export

### To Application Insights

```csharp
var properties = new Dictionary<string, string>
{
    { "RequestId", result.Metrics.RequestId ?? "unknown" },
    { "Model", result.Metrics.Model },
    { "FinishReason", result.Metrics.FinishReason }
};

var metrics = new Dictionary<string, double>
{
    { "Tokens/Prompt", result.Metrics.PromptTokens },
    { "Tokens/Completion", result.Metrics.CompletionTokens },
    { "Duration/Total", result.Metrics.TotalDurationMs },
    { "Duration/Network", result.Metrics.RequestDurationMs },
    { "Duration/Processing", result.Metrics.ProcessingDurationMs },
    { "Cost/USD", (double)result.Metrics.EstimatedCostUsd },
    { "InputSize/Bytes", result.Metrics.InputSizeBytes }
};

_telemetryClient.TrackEvent("InvoiceExtractionCompleted", properties, metrics);
```

### To Prometheus

```csharp
var labels = new[] { result.Metrics.Model };
_tokensHistogram.Labels(labels).Observe(result.Metrics.TotalTokens);
_durationHistogram.Labels(labels).Observe(result.Metrics.TotalDurationMs);
_costGauge.Labels(labels).Set((double)result.Metrics.EstimatedCostUsd);
```

## Troubleshooting with Metrics

### High Response Times
- Check `ProcessingDurationMs` vs `RequestDurationMs`
- If processing is high: Model overloaded or complex input
- If network is high: Network latency issues

### Unexpected Token Usage
- Compare `PromptTokens` across similar invoices
- Large variance may indicate tokenization issues
- Check invoice size: `InputSizeBytes`

### Cost Anomalies
- Monitor `EstimatedCostUsd` trends
- Compare with baseline: typical tokens per invoice
- Investigate if `CompletionTokens` unusually high

### API Errors
- Use `RequestId` to trace in OpenAI logs
- Check `HttpStatusCode` and `ErrorMessage`
- Review `RequestTimestampUtc` for rate limit windows

## Performance Benchmarks (Expected)

| Metric | Expected Range | Notes |
|--------|-----------------|-------|
| Total Duration | 800-1500ms | Includes network latency |
| Prompt Tokens | 300-800 | Depends on invoice size |
| Completion Tokens | 50-200 | Relatively consistent |
| Estimated Cost | $0.002-$0.008 | Per invoice |
| Success Rate | 95%+ | With well-formed invoices |

---

For questions or issues, refer to the README.md or QUICKSTART.md files.
