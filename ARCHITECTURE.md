# Architecture Overview with Observability

## Component Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     Application Layer                            │
│                      Program.cs                                  │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  Console Application                                        │ │
│  │  - Load API key from Settings.json                         │ │
│  │  - Call service with sample invoice                        │ │
│  │  - Display results and metrics                             │ │
│  └────────────────────────────────────────────────────────────┘ │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Service Interface Layer                       │
│                    IInvoiceParser (Interface)                    │
│                    + InvoiceParserExtensions                     │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  ExtractInvoiceAsync()                                     │ │
│  │    Returns: Task<ExtractionResult>                         │ │
│  │                                                            │ │
│  │  ExtractInvoiceSimpleAsync() [Extension]                  │ │
│  │    Returns: Task<InvoiceExtractionResult?>                │ │
│  └────────────────────────────────────────────────────────────┘ │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Service Implementation Layer                    │
│               OpenAIInvoiceService (implements)                  │
│                                                                  │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │  1. Prepare Request                                        │ │
│  │     - System prompt + user message                         │ │
│  │     - JSON mode configuration                              │ │
│  │     - Start timers (Stopwatch)                             │ │
│  │     - Calculate input size                                 │ │
│  │                                                            │ │
│  │  2. Send to OpenAI API                                     │ │
│  │     - HTTP POST to /v1/chat/completions                   │ │
│  │     - Track network latency                                │ │
│  │                                                            │ │
│  │  3. Parse Response                                         │ │
│  │     - Extract JSON body                                    │ │
│  │     - Parse usage tokens                                   │ │
│  │     - Calculate cost                                       │ │
│  │     - Extract request ID                                   │ │
│  │                                                            │ │
│  │  4. Build Result                                           │ │
│  │     - Deserialize invoice data                             │ │
│  │     - Collect all metrics                                  │ │
│  │     - Return ExtractionResult wrapper                      │ │
│  │                                                            │ │
│  │  5. Error Handling                                         │ │
│  │     - Catch exceptions                                     │ │
│  │     - Return failure result with metrics                   │ │
│  │     - Preserve request ID for tracing                      │ │
│  └────────────────────────────────────────────────────────────┘ │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Data Models Layer                             │
│                                                                  │
│  ┌──────────────────────┐     ┌──────────────────────────────┐ │
│  │ ExtractionResult     │     │ ExtractionMetrics            │ │
│  │                      │     │                              │ │
│  │ - Data              │────▶│ - PromptTokens               │ │
│  │   InvoiceExtraction │     │ - CompletionTokens           │ │
│  │   Result            │     │ - TotalTokens                │ │
│  │                      │     │ - RequestDurationMs          │ │
│  │ - Metrics           │     │ - ProcessingDurationMs       │ │
│  │   ExtractionMetrics │     │ - TotalDurationMs            │ │
│  │                      │     │ - HttpStatusCode             │ │
│  │ - Success (bool)    │     │ - Model                      │ │
│  │                      │     │ - RequestId                  │ │
│  │                      │     │ - RequestTimestampUtc        │ │
│  │                      │     │ - EstimatedCostUsd           │ │
│  │                      │     │ - InputSizeBytes             │ │
│  │                      │     │ - ResponseSizeBytes          │ │
│  │                      │     │ - IsSuccessful               │ │
│  │                      │     │ - ErrorMessage               │ │
│  │                      │     │ - FinishReason               │ │
│  │                      │     │ - ToString()                 │ │
│  └──────────────────────┘     └──────────────────────────────┘ │
│                                                                  │
│  ┌──────────────────────┐     ┌──────────────────────────────┐ │
│  │ InvoiceExtraction    │     │ LineItem                     │ │
│  │ Result               │     │                              │ │
│  │                      │     │ - Description (string)       │ │
│  │ - InvoiceNumber      │────▶│ - Amount (decimal)           │ │
│  │ - VendorName         │     └──────────────────────────────┘ │
│  │ - InvoiceDate        │                                       │
│  │ - TotalAmount        │                                       │
│  │ - LineItems          │                                       │
│  └──────────────────────┘                                       │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│                 External APIs & Services                         │
│                                                                  │
│  OpenAI API (api.openai.com)                                    │
│  ├─ GPT-4o Model (gpt-4o-2024-08-06)                           │
│  ├─ Endpoint: /v1/chat/completions                             │
│  ├─ Features: Structured Outputs (JSON Mode)                   │
│  └─ Returns: usage info, content, finish_reason                │
└─────────────────────────────────────────────────────────────────┘
```

## Data Flow Diagram

```
User Input
    │
    ▼
┌─────────────────────────────────────┐
│  OpenAIInvoiceService               │
│  .ExtractInvoiceAsync(invoiceText)  │
└─────────────────────────────────────┘
    │
    ├─ Start: Stopwatch (Total Duration)
    ├─ Measure: Input Size
    │
    ▼
┌─────────────────────────────────────┐
│  Build Request                      │
│  - System prompt                    │
│  - User message + invoice text      │
│  - JSON mode config                 │
└─────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────┐
│  Start: Network Stopwatch           │
│  Send HTTP POST to OpenAI           │
└─────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────┐
│  OpenAI Server Processing           │
│  - Process request                  │
│  - Generate response                │
│  - Calculate tokens                 │
└─────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────┐
│  Stop: Network Stopwatch            │
│  Network Time = Time taken          │
│                                     │
│  Receive Response:                  │
│  {                                  │
│    "id": "chatcmpl-...",            │
│    "usage": {                       │
│      "prompt_tokens": 300,          │
│      "completion_tokens": 50,       │
│      "total_tokens": 350            │
│    },                               │
│    "choices": [{                    │
│      "message": {                   │
│        "content": "{...json...}"    │
│      },                             │
│      "finish_reason": "stop"        │
│    }]                               │
│  }                                  │
└─────────────────────────────────────┘
    │
    ├─ Extract: JSON content, tokens, request ID, finish reason
    ├─ Calculate: Processing time (total - network)
    ├─ Calculate: Estimated cost
    │
    ▼
┌─────────────────────────────────────┐
│  Deserialize JSON to                │
│  InvoiceExtractionResult            │
│  - VendorName, InvoiceDate, etc     │
└─────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────┐
│  Collect Metrics                    │
│  - All token counts                 │
│  - All timing info                  │
│  - All size info                    │
│  - Status and IDs                   │
└─────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────┐
│  Build ExtractionResult             │
│  {                                  │
│    Data: InvoiceExtractionResult,   │
│    Metrics: ExtractionMetrics       │
│  }                                  │
└─────────────────────────────────────┘
    │
    ▼
Return to Caller
```

## Metrics Collection Points

```
Timeline of Metrics Collection
────────────────────────────────────────────────────────────────

┌─ T0: Operation Start
│   └─ RequestTimestampUtc = now
│   └─ Start Total Stopwatch
│   └─ InputSizeBytes = measure(invoiceText)
│
├─ T1: Before Network Call
│   └─ Start Network Stopwatch
│
├─ T2: After Network Response
│   └─ Stop Network Stopwatch
│   └─ RequestDurationMs = elapsed
│   └─ HttpStatusCode = response.StatusCode
│   └─ ResponseSizeBytes = measure(response)
│
├─ T3: Parse Response JSON
│   └─ Extract: PromptTokens, CompletionTokens, TotalTokens
│   └─ Extract: RequestId
│   └─ Extract: FinishReason
│   └─ Extract: Response content (invoice JSON)
│
├─ T4: Deserialize Invoice Data
│   └─ Parse invoice JSON
│   └─ Check for errors
│
├─ T5: Calculate Metrics
│   └─ Stop Total Stopwatch
│   └─ TotalDurationMs = total elapsed
│   └─ ProcessingDurationMs = TotalDurationMs - RequestDurationMs
│   └─ EstimatedCostUsd = calculate(tokens)
│   └─ IsSuccessful = true/false
│   └─ ErrorMessage = if failed
│
└─ T6: Return ExtractionResult
    └─ Contains Data (invoice) + Metrics (all collected info)
```

## Cost Calculation Flow

```
PromptTokens (e.g., 800)
    │
    ├─ * (800 / 1,000,000)
    │
    ▼
(0.0008)
    │
    ├─ * $2.50 per 1M input tokens
    │
    ▼
InputCost = $0.002
    │
    │
CompletionTokens (e.g., 200)
    │
    ├─ * (200 / 1,000,000)
    │
    ▼
(0.0002)
    │
    ├─ * $10.00 per 1M output tokens
    │
    ▼
OutputCost = $0.002
    │
    │
    ├─ InputCost + OutputCost
    │
    ▼
EstimatedCostUsd = $0.004
```

## Error Handling with Observability

```
Try ExtractInvoiceAsync
    │
    ├─ Start capturing metrics
    │
    ├─ Success Path
    │   └─ Return ExtractionResult {
    │       Data: InvoiceExtractionResult,
    │       Metrics: { IsSuccessful: true, ... }
    │     }
    │
    └─ Failure Path (Any Exception)
        ├─ Catch and record error
        ├─ Return ExtractionResult {
        │   Data: null,
        │   Metrics: {
        │     IsSuccessful: false,
        │     ErrorMessage: "details",
        │     HttpStatusCode: (if HTTP error),
        │     RequestId: (if available),
        │     TotalDurationMs: (time spent before failure),
        │     ...other metrics so far...
        │   }
        │ }
```

## Integration Points

### Logging Integration
```
Logger.LogInformation(
    "Extraction completed: RequestId={RequestId}, " +
    "Tokens={TotalTokens}, Duration={Duration}ms, Cost=${Cost}",
    metrics.RequestId,
    metrics.TotalTokens,
    metrics.TotalDurationMs,
    metrics.EstimatedCostUsd
)
```

### Monitoring Integration
```
Metrics.RecordGauge("extraction.tokens", metrics.TotalTokens)
Metrics.RecordGauge("extraction.duration_ms", metrics.TotalDurationMs)
Metrics.RecordGauge("extraction.cost_usd", metrics.EstimatedCostUsd)
```

### Tracing Integration
```
using (var activity = new Activity("InvoiceExtraction").Start())
{
    activity.SetTag("request_id", metrics.RequestId)
    activity.SetTag("tokens", metrics.TotalTokens)
    activity.SetTag("duration", metrics.TotalDurationMs)
    // ...
}
```

---

For detailed examples and implementation patterns, see:
- `README.md` - Project overview
- `QUICKSTART.md` - Getting started
- `OBSERVABILITY.md` - Metrics and monitoring
- `OBSERVABILITY-UPDATE.md` - What changed
