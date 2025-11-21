# Invoice Extraction Service - LLM Integration

A production-ready C# service for extracting structured data from raw invoice text using OpenAI's GPT-4o model with a comprehensive evaluation suite focused on **Probabilistic Testing** and **Evals**.

## Architecture Overview

### 1. **Models (DTOs)**

#### `InvoiceExtractionResult` (Record)
Represents the extracted invoice data with strong typing:
- `InvoiceNumber` (string?): The extracted invoice number
- `VendorName` (string?): The extracted vendor/supplier name
- `InvoiceDate` (DateTime?): The invoice date
- `TotalAmount` (decimal): The total invoice amount
- `LineItems` (List<LineItem>): Collection of line items

#### `LineItem` (Record)
Represents individual line items:
- `Description` (string): Item description
- `Amount` (decimal): Item amount

### 2. **Service Layer**

#### `IInvoiceParser` (Interface)
Defines the contract for invoice extraction:
```csharp
Task<InvoiceExtractionResult> ExtractInvoiceAsync(string invoiceText, CancellationToken cancellationToken = default);
```

#### `OpenAIInvoiceService` (Implementation)
- Uses OpenAI's GPT-4o model (`gpt-4o-2024-08-06`)
- Implements **Structured Outputs** (JSON Mode) for deterministic responses
- System Prompt: *"You are a financial data extraction assistant. Extract data strictly. If a field is missing, return null."*
- Ensures strict JSON schema compliance
- Error handling with descriptive exceptions

### 3. **Evaluation Suite (Probabilistic Testing)**

The `InvoiceExtractionEvals` class implements comprehensive quality gates:

#### **A. Golden Dataset** (`GetGoldenInvoices`)
5 parameterized test cases covering:
- Standard invoices with complete data
- Vendor name variations (case differences, abbreviations)
- Minimal invoice formats
- Decimal precision handling
- OCR-like variations and typos

Each case provides:
- `input_text`: Raw invoice text
- `expected_vendor`: Ground truth vendor name
- `expected_total`: Ground truth total amount

#### **B. Consistency Eval** (`Evaluate_InternalConsistency`)
**Purpose**: Hallucination Detection
- Validates that sum of `LineItems.Amount` equals `TotalAmount`
- Allows delta of ±0.01 for rounding errors
- Detects when LLM generates inconsistent totals

**Theory Test**: Runs across all 5 golden invoices

#### **C. Accuracy Eval** (`Evaluate_VendorAccuracy`)
**Purpose**: Fuzzy Matching with OCR Error Tolerance
- Uses `CalculateLevenshteinDistance()` helper function
- Levenshtein distance threshold: ≤ 3 characters
- Allows small OCR errors: "Inc." ↔ "Inc", typos, case variations
- Detects vendor name extraction quality

**Theory Test**: Runs across all 5 golden invoices

#### **D. Format Eval** (`Evaluate_DateValidity`)
**Purpose**: Data Format and Reasonableness Validation
- Asserts `InvoiceDate` is not null
- Asserts `InvoiceDate` is not in the future (1-day tolerance)
- Asserts `InvoiceDate` year >= 2000 (sanity check)
- Detects hallucinated or invalid dates

**Theory Test**: Runs across all 5 golden invoices

### 4. **Utilities**

#### `StringDistance.CalculateLevenshteinDistance()`
Calculates minimum edit distance between two strings:
- Handles null/empty cases
- Case-insensitive comparison
- 2D dynamic programming implementation
- O(n*m) time complexity where n, m are string lengths

## Project Structure

```
LLM-Integration/
├── LLM-Integration.csproj
├── Program.cs
├── Settings.json
├── Models/
│   ├── InvoiceExtractionResult.cs
│   └── LineItem.cs
└── Services/
    ├── IInvoiceParser.cs
    └── OpenAIInvoiceService.cs

LLM-Integration.Tests/
├── LLM-Integration.Tests.csproj
├── Evals/
│   └── InvoiceExtractionEvals.cs
└── Utilities/
    └── StringDistance.cs
```

## Setup Instructions

### Prerequisites
- .NET 9.0+
- OpenAI API key (GPT-4o access required)

### Configuration

1. **Update `Settings.json`**:
```json
{
    "API-Key": "your-openai-api-key-here"
}
```

2. **Build Solution**:
```bash
dotnet build
```

3. **Run Tests**:
```bash
# Run all evaluation tests
dotnet test LLM-Integration.Tests/

# Run specific test class
dotnet test LLM-Integration.Tests/ --filter "ClassName=LLM_Integration.Tests.Evals.InvoiceExtractionEvals"

# Run with verbose output
dotnet test LLM-Integration.Tests/ --logger "console;verbosity=detailed"
```

### Example Usage

```csharp
using LLM_Integration.Services;

var apiKey = "your-openai-api-key";
var service = new OpenAIInvoiceService(apiKey);

var invoiceText = """
    INVOICE INV-2024-001
    Vendor: ACME Corp
    Date: 2024-11-15
    Items:
    - Widget: $100.00
    - Service: $50.00
    Total: $150.00
    """;

var result = await service.ExtractInvoiceAsync(invoiceText);

Console.WriteLine($"Vendor: {result.VendorName}");
Console.WriteLine($"Total: ${result.TotalAmount}");
foreach (var item in result.LineItems)
{
    Console.WriteLine($"  - {item.Description}: ${item.Amount}");
}
```

## Key Design Decisions

### 1. **Structured Outputs (JSON Mode)**
- Ensures deterministic JSON responses from GPT-4o
- Eliminates free-form text parsing ambiguity
- Guarantees schema compliance

### 2. **Probabilistic Testing Framework**
- Golden dataset approach for regression testing
- Theory-based tests (xUnit) for parameterized validation
- Multiple evaluation angles (consistency, accuracy, format)

### 3. **Fuzzy Matching for Vendor Names**
- Levenshtein distance handles OCR errors
- Threshold of 3 allows realistic variances
- Example: "ACME Inc." → "Acme Inc" is 1 edit

### 4. **Mock Service for Tests**
- Avoids API call costs during testing
- Provides predictable, deterministic results
- Faster feedback loop for development

## Evaluation Metrics Explained

| Evaluation | Purpose | Method | Threshold |
|-----------|---------|--------|-----------|
| Internal Consistency | Hallucination Detection | Sum(LineItems) == TotalAmount | ±0.01 delta |
| Vendor Accuracy | Fuzzy Name Matching | Levenshtein Distance | ≤ 3 characters |
| Date Validity | Format & Sanity | Date checks | Not null, not future, year ≥ 2000 |

## Running the Application

```bash
# Build and run the console app
dotnet run --project LLM-Integration/

# This will attempt to extract invoice data from a sample invoice
# Requires Settings.json with valid OpenAI API key
```

## Testing Strategy

### Golden Dataset Approach
- Manually curated test cases with known good outputs
- Covers edge cases: variations in vendor names, OCR errors, precision
- Provides ground truth for accuracy measurement

### Theory-Based Tests
- Each evaluation runs against all 5 golden invoices
- Total of 15 test cases (3 evals × 5 invoices)
- Parallel execution via xUnit

### Extensibility
To add new test cases:
1. Add new `yield return` statement in `GetGoldenInvoices()`
2. All four evaluation tests automatically run against the new case

## Future Enhancements

- [ ] Integration with real OpenAI API in integration tests
- [ ] Performance benchmarking (response time, cost tracking)
- [ ] Confidence scores for extracted fields
- [ ] Support for multiple invoice formats (scanned images via OCR)
- [ ] Database persistence for audit trails
- [ ] Async batch processing for bulk extractions
- [ ] Custom evaluation metrics per customer
- [ ] Cost optimization (switching between GPT-4o and GPT-4o mini)

## License

MIT License

## Support

For issues or questions, please refer to the OpenAI API documentation:
- https://platform.openai.com/docs/guides/structured-outputs
- https://platform.openai.com/docs/models/gpt-4o
