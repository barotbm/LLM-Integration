# Transcript Analysis Service

## Overview

The `TranscriptAnalysisService` analyzes mortgage servicing call transcripts for compliance issues and quality metrics using OpenAI's GPT-4o model. It follows the same architectural patterns as the `InvoiceExtractionService` and includes comprehensive observability metrics.

## Features

✅ **Compliance Auditing** - Identifies regulatory violations and policy breaches  
✅ **Quality Scoring** - Provides an overall quality score (1-10)  
✅ **Structured Output** - Returns compliance issues with severity levels  
✅ **Full Observability** - Token usage, response times, and cost tracking  
✅ **Enterprise Guardrails** - Built-in PII protection and fair lending compliance  

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│              ITranscriptAnalysisService                      │
│  AnalyzeTranscriptAsync(transcriptText) → ExtractionResult  │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           │ implements
                           ▼
┌─────────────────────────────────────────────────────────────┐
│           TranscriptAnalysisService                          │
│  • System Prompt with Compliance Guardrails                 │
│  • GPT-4o with Structured Outputs (JSON Mode)               │
│  • Token & Cost Tracking                                     │
│  • Response Time Measurement                                 │
└──────────────────────────┬──────────────────────────────────┘
                           │
                           │ returns
                           ▼
┌─────────────────────────────────────────────────────────────┐
│   ExtractionResult<TranscriptAnalysisResult>                │
│   ├── Data: TranscriptAnalysisResult                        │
│   │   ├── ComplianceIssues: List<ComplianceIssue>          │
│   │   ├── OverallScore: int (1-10)                          │
│   │   └── Summary: string                                   │
│   └── Metrics: ExtractionMetrics                            │
│       ├── Tokens (prompt, completion, total)                │
│       ├── Duration (network, processing, total)             │
│       ├── Cost (estimated USD)                              │
│       └── Request metadata (ID, status, timestamp)          │
└─────────────────────────────────────────────────────────────┘
```

## Usage

### Basic Usage

```csharp
using LLM_Integration.Services;

var apiKey = "your-openai-api-key";
var service = new TranscriptAnalysisService(apiKey);

var transcript = """
    Agent: Good morning! This is Sarah from FinTech Mortgage Solutions.
    Customer: Hi, I need help with my mortgage payment.
    Agent: I'd be happy to help. Can I get your loan number?
    """;

var result = await service.AnalyzeTranscriptAsync(transcript);

if (result.Success)
{
    Console.WriteLine($"Score: {result.Data!.OverallScore}/10");
    Console.WriteLine($"Summary: {result.Data.Summary}");
    
    foreach (var issue in result.Data.ComplianceIssues)
    {
        Console.WriteLine($"[{issue.Severity}] {issue.Description}");
    }
    
    // Access metrics
    Console.WriteLine($"Tokens: {result.Metrics.TotalTokens}");
    Console.WriteLine($"Cost: ${result.Metrics.EstimatedCostUsd:F6}");
}
```

### Simple Usage (Without Metrics)

```csharp
var result = await service.AnalyzeTranscriptSimpleAsync(transcript);

if (result != null)
{
    Console.WriteLine($"Score: {result.OverallScore}/10");
}
```

### With Cancellation Token

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
var result = await service.AnalyzeTranscriptAsync(transcript, cts.Token);
```

## System Prompt

The service uses a comprehensive system prompt with the following sections:

### 1. Identity & Role
- **Role**: Mortgage QA Analyst
- **Company**: FinTech Mortgage Solutions
- **Domain**: Mortgage Servicing
- **Objective**: Audit agent-customer interactions for compliance and empathy

### 2. Context & Knowledge Base
- Analyzes only the provided transcript text
- No external knowledge or assumptions
- Explicit "cannot answer" for out-of-scope queries

### 3. Compliance & Guardrails (CRITICAL)
- ✅ No financial advice or market predictions
- ✅ PII protection (no SSNs, CVVs, account numbers)
- ✅ Regulatory tone (no absolute guarantees)
- ✅ Fair lending (no prohibited basis factors)

### 4. Tone & Voice
- Professional and objective
- Empathetic but firm
- Explains financial acronyms (DTI, APR, LTV)

### 5. Reasoning Process
- Analyzes user intent
- Verifies compliance violations
- Searches context for evidence
- Formulates answer with citations

### 6. Output Format
Returns structured JSON with:
- `ComplianceIssues`: Array of issues with description and severity
- `OverallScore`: Integer from 1-10
- `Summary`: String summarizing the analysis

## Data Models

### TranscriptAnalysisResult

```csharp
public record TranscriptAnalysisResult
{
    public List<ComplianceIssue> ComplianceIssues { get; init; }
    public int OverallScore { get; init; }
    public string? Summary { get; init; }
}
```

### ComplianceIssue

```csharp
public record ComplianceIssue
{
    public required string Description { get; init; }
    public required string Severity { get; init; } // "Low", "Medium", "High"
}
```

### ExtractionResult<T>

```csharp
public record ExtractionResult<T> where T : class
{
    public T? Data { get; init; }
    public required ExtractionMetrics Metrics { get; init; }
    public bool Success => Data != null && Metrics.IsSuccessful;
}
```

## Observability Metrics

Every analysis call returns comprehensive metrics:

| Metric | Description |
|--------|-------------|
| `PromptTokens` | Tokens in the system + user prompt |
| `CompletionTokens` | Tokens in the model's response |
| `TotalTokens` | Sum of prompt + completion tokens |
| `RequestDurationMs` | Network latency (API call time) |
| `ProcessingDurationMs` | Server-side processing time |
| `TotalDurationMs` | End-to-end duration |
| `EstimatedCostUsd` | Calculated cost based on token usage |
| `HttpStatusCode` | HTTP response code |
| `Model` | Model name (gpt-4o-2024-08-06) |
| `RequestId` | OpenAI request ID for tracing |
| `FinishReason` | Completion reason (stop, length, etc.) |
| `InputSizeBytes` | Size of input transcript |
| `ResponseSizeBytes` | Size of API response |
| `IsSuccessful` | Whether analysis succeeded |
| `ErrorMessage` | Error details if failed |
| `RequestTimestampUtc` | When the request was made |

## Cost Calculation

The service automatically calculates costs based on OpenAI's GPT-4o pricing:

- **Input tokens**: $2.50 per 1M tokens
- **Output tokens**: $10.00 per 1M tokens

Example cost calculation:
```
Prompt: 500 tokens × ($2.50 / 1M) = $0.001250
Completion: 200 tokens × ($10.00 / 1M) = $0.002000
Total: $0.003250
```

## Error Handling

The service handles various error scenarios gracefully:

```csharp
var result = await service.AnalyzeTranscriptAsync(transcript);

if (!result.Success)
{
    Console.WriteLine($"Analysis failed: {result.Metrics.ErrorMessage}");
    Console.WriteLine($"HTTP Status: {result.Metrics.HttpStatusCode}");
    Console.WriteLine($"Duration: {result.Metrics.TotalDurationMs}ms");
    // Metrics are still available even on failure
}
```

Common error scenarios:
- Invalid API key → HTTP 401
- Timeout → OperationCanceledException
- Network issues → HttpRequestException
- Invalid JSON response → Deserialization error

## Testing

### Unit Tests

```bash
dotnet test --filter TranscriptAnalysisServiceTests
```

### Integration Tests

For end-to-end testing with the real API:

```bash
# Set API key
$env:OPENAI_API_KEY = "your-api-key"

# Run demo
dotnet run --project LLM-Integration
# Select option 2 for Transcript Analysis
```

## Sample Output

```
╔════════════════════════════════════════════════════════════════╗
║                   TRANSCRIPT ANALYSIS DEMO                     ║
╚════════════════════════════════════════════════════════════════╝

Analyzing transcript for compliance and quality...

✓ ANALYSIS COMPLETE

Overall Score: 9/10

Summary:
The agent demonstrated excellent compliance and empathy. They followed proper
PII protection protocols by not requesting sensitive information over the phone.
The disclaimer about information being for informational purposes only was
properly provided. The agent offered helpful forbearance options while
maintaining a professional and empathetic tone throughout the interaction.

⚠ Compliance Issues Found: 1
  🟡 [Medium] Agent should have confirmed customer identity using security
              questions before discussing account details.

--- Observability Metrics ---
Tokens:     1,234 (prompt: 876, completion: 358)
Duration:   1,450ms (network: 1,200ms)
Cost:       $0.005780
Request ID: chatcmpl-abc123xyz
```

## Compliance Features

### PII Protection
The system prompt explicitly instructs the model to:
- Never output full SSNs, CVVs, or bank account numbers
- Flag transcripts where agents request sensitive data over unsecured channels

### Fair Lending Compliance
The model is instructed to:
- Never consider prohibited basis factors (race, religion, sex, age, etc.)
- Flag any discriminatory language in transcripts

### Regulatory Tone
The model ensures:
- No absolute guarantees ("you will be approved")
- Proper use of conditional language ("you may be eligible")
- Appropriate disclaimers for financial information

### Financial Advice Protection
The model verifies:
- No personalized investment advice
- No market predictions
- Proper "informational purposes only" disclaimers

## Performance

Typical metrics for transcript analysis:

| Metric | Value |
|--------|-------|
| Average Tokens | 800-1,500 |
| Average Duration | 1,000-2,000ms |
| Average Cost | $0.003-$0.008 |
| Network Time | ~80% of total |
| Processing Time | ~20% of total |

## Integration Examples

### Batch Processing

```csharp
var transcripts = LoadTranscripts();
var results = new List<ExtractionResult<TranscriptAnalysisResult>>();

foreach (var transcript in transcripts)
{
    var result = await service.AnalyzeTranscriptAsync(transcript);
    results.Add(result);
    
    // Track costs
    Console.WriteLine($"Cost: ${result.Metrics.EstimatedCostUsd:F6}");
}

var totalCost = results.Sum(r => r.Metrics.EstimatedCostUsd);
Console.WriteLine($"Total batch cost: ${totalCost:F4}");
```

### With Retry Logic

```csharp
var maxRetries = 3;
var retryCount = 0;

while (retryCount < maxRetries)
{
    var result = await service.AnalyzeTranscriptAsync(transcript);
    
    if (result.Success)
    {
        return result;
    }
    
    if (result.Metrics.HttpStatusCode == 429) // Rate limit
    {
        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
        retryCount++;
    }
    else
    {
        throw new Exception(result.Metrics.ErrorMessage);
    }
}
```

### Monitoring Integration

```csharp
var result = await service.AnalyzeTranscriptAsync(transcript);

// Send to Application Insights
telemetry.TrackEvent("TranscriptAnalysis", new Dictionary<string, string>
{
    ["Score"] = result.Data?.OverallScore.ToString() ?? "0",
    ["IssueCount"] = result.Data?.ComplianceIssues.Count.ToString() ?? "0",
    ["Tokens"] = result.Metrics.TotalTokens.ToString(),
    ["Duration"] = result.Metrics.TotalDurationMs.ToString(),
    ["Cost"] = result.Metrics.EstimatedCostUsd.ToString("F6")
});
```

## Best Practices

1. **Always check Success flag** before accessing Data
2. **Monitor token usage** to control costs
3. **Set reasonable timeouts** (30-60 seconds recommended)
4. **Log request IDs** for debugging with OpenAI support
5. **Aggregate metrics** for batch processing insights
6. **Handle rate limits** with exponential backoff
7. **Validate severity levels** in post-processing if needed
8. **Redact PII** from transcripts before analysis if not needed for compliance checks

## Troubleshooting

### High Token Usage

If token counts are higher than expected:
- The transcript may contain repetitive content
- The model may be generating verbose summaries
- Check `PromptTokens` vs `CompletionTokens` to identify source

### Low Quality Scores

If scores are consistently low:
- Review actual compliance issues to understand patterns
- Consider if transcripts need pre-processing (formatting, redaction)
- Verify agent training aligns with compliance requirements

### Slow Response Times

If `TotalDurationMs` is high:
- Check `RequestDurationMs` (network) vs `ProcessingDurationMs` (API)
- High network time → connectivity issues
- High processing time → complex analysis or model capacity

### JSON Deserialization Errors

If the response can't be parsed:
- The model may have generated invalid JSON (rare with GPT-4o)
- Check `ResponseSizeBytes` to ensure response wasn't truncated
- Review `FinishReason` - should be "stop", not "length"

## Support

For issues or questions:
- Review logs with `RequestId` when contacting OpenAI support
- Check `ErrorMessage` in metrics for detailed error information
- Verify API key permissions and quota limits
- Ensure network connectivity to `api.openai.com`

## License

This service is part of the LLM-Integration project.
