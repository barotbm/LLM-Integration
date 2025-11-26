# 🚀 Quick Start - Transcript Analysis Service

## Get Started in 2 Minutes

### 1. Set Your API Key

Edit `LLM-Integration/Settings.json`:
```json
{
    "API-Key": "sk-your-openai-api-key-here"
}
```

### 2. Run the Demo

```powershell
cd LLM-Integration
dotnet run
```

Select option **2** (Transcript Analysis) or **3** (Both services)

### 3. See It Work

```
╔════════════════════════════════════════════════════════════════╗
║                   TRANSCRIPT ANALYSIS DEMO                     ║
╚════════════════════════════════════════════════════════════════╝

Analyzing transcript for compliance and quality...

✓ ANALYSIS COMPLETE

Overall Score: 9/10

Summary:
The agent demonstrated excellent compliance and empathy. They followed 
proper PII protection protocols...

⚠ Compliance Issues Found: 1
  🟡 [Medium] Agent should have confirmed customer identity using 
              security questions before discussing account details.

--- Observability Metrics ---
Tokens:     1,234 (prompt: 876, completion: 358)
Duration:   1,450ms (network: 1,200ms)
Cost:       $0.005780
Request ID: chatcmpl-abc123xyz
```

---

## 📝 Basic Code Example

```csharp
using LLM_Integration.Services;

// Initialize the service
var apiKey = "your-api-key";
var service = new TranscriptAnalysisService(apiKey);

// Analyze a transcript
var transcript = """
    Agent: Good morning! This is Sarah from FinTech Mortgage Solutions.
    Customer: Hi, I need help with my mortgage payment.
    Agent: I'd be happy to help. Can I get your loan number?
    Customer: It's 123456789.
    Agent: Thank you. Let me pull up your account...
    """;

var result = await service.AnalyzeTranscriptAsync(transcript);

// Check results
if (result.Success)
{
    Console.WriteLine($"Score: {result.Data!.OverallScore}/10");
    Console.WriteLine($"Summary: {result.Data.Summary}");
    
    Console.WriteLine($"\nCompliance Issues: {result.Data.ComplianceIssues.Count}");
    foreach (var issue in result.Data.ComplianceIssues)
    {
        Console.WriteLine($"  [{issue.Severity}] {issue.Description}");
    }
    
    // Check metrics
    Console.WriteLine($"\nTokens Used: {result.Metrics.TotalTokens}");
    Console.WriteLine($"Cost: ${result.Metrics.EstimatedCostUsd:F6}");
    Console.WriteLine($"Duration: {result.Metrics.TotalDurationMs}ms");
}
else
{
    Console.WriteLine($"Analysis failed: {result.Metrics.ErrorMessage}");
}
```

---

## 🎯 What Gets Analyzed

The service checks for:

### ✅ Compliance Issues
- PII protection violations (SSNs, CVVs, account numbers)
- Missing disclaimers
- Absolute guarantees (regulatory tone)
- Discriminatory language (fair lending)
- Financial advice violations

### ✅ Quality Metrics
- Agent empathy and professionalism
- Proper call flow and structure
- Clear communication
- Problem resolution effectiveness

### ✅ Output Structure
- **OverallScore**: 1-10 rating
- **Summary**: Text summary of the call
- **ComplianceIssues**: List of problems with severity (Low/Medium/High)

---

## 💰 Cost Tracking

Every call returns cost information:

```csharp
var result = await service.AnalyzeTranscriptAsync(transcript);
Console.WriteLine($"This call cost: ${result.Metrics.EstimatedCostUsd:F6}");
```

**Typical costs**:
- Short transcript (500 tokens): ~$0.003
- Medium transcript (1000 tokens): ~$0.005
- Long transcript (2000 tokens): ~$0.008

---

## ⏱️ Performance

Typical analysis times:
- Network latency: 800-1,200ms
- Processing time: 200-400ms
- **Total: 1,000-1,600ms**

---

## 🧪 Testing

Run the unit tests:
```bash
dotnet test --filter TranscriptAnalysisServiceTests
```

All tests should pass:
- ✅ Constructor validation
- ✅ Argument validation
- ✅ Model structure tests

---

## 📚 Learn More

- **Detailed Documentation**: See `TRANSCRIPT-ANALYSIS.md`
- **Implementation Summary**: See `TRANSCRIPT-IMPLEMENTATION-SUMMARY.md`
- **Architecture**: See the service interface and implementation files

---

## 🛠️ Troubleshooting

### "API key not configured"
→ Add your OpenAI API key to `Settings.json`

### "Request timeout"
→ Add a cancellation token:
```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
var result = await service.AnalyzeTranscriptAsync(transcript, cts.Token);
```

### "High cost"
→ Check transcript length and token usage:
```csharp
Console.WriteLine($"Tokens: {result.Metrics.TotalTokens}");
Console.WriteLine($"Input size: {result.Metrics.InputSizeBytes} bytes");
```

---

## 🎉 That's It!

You now have a production-ready transcript analysis service with:
- ✅ Compliance checking
- ✅ Quality scoring
- ✅ Full observability
- ✅ Cost tracking

**Next**: Try it with your own transcripts!
