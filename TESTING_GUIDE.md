# Testing Guide - Invoice Extraction Evals

## Overview

The `InvoiceExtractionEvals` class provides a comprehensive evaluation suite using **probabilistic testing** principles. This guide explains how the evals work and how to extend them.

## Evaluation Architecture

### 1. Golden Dataset Pattern

The golden dataset approach provides:
- **Deterministic Test Cases**: Known ground truth values
- **Regression Testing**: Detect quality regressions over time
- **Parametric Testing**: Same tests run across multiple cases automatically

```csharp
public static IEnumerable<object[]> GetGoldenInvoices()
{
    yield return new object[] { invoiceText1, expectedVendor1, expectedTotal1 };
    yield return new object[] { invoiceText2, expectedVendor2, expectedTotal2 };
    // ... more cases
}
```

**xUnit Magic**: Using `[MemberData(nameof(GetGoldenInvoices))]` automatically:
- Creates a test case for each `yield return`
- Runs all 3 evaluation methods against each case
- **Result**: 15 total test cases from 3 methods × 5 invoices

### 2. Theory-Based Testing

Each evaluation uses xUnit's `[Theory]` attribute:

```csharp
[Theory]
[MemberData(nameof(GetGoldenInvoices))]
public void Evaluate_InternalConsistency(string invoiceText, string expectedVendor, decimal expectedTotal)
{
    // Arrange
    var mockService = CreateMockInvoiceService(invoiceText, expectedVendor, expectedTotal);
    
    // Act
    var result = mockService.ExtractInvoiceAsync(invoiceText).Result;
    
    // Assert
    Assert.True(/* condition */);
}
```

**Why Theory Tests?**
- One test definition, multiple executions
- Automatic naming: `Evaluate_InternalConsistency(invoiceText1, expectedVendor1, expectedTotal1)`
- Failed case is immediately identifiable

## Evaluation Details

### Evaluation A: Internal Consistency (Hallucination Check)

**What**: Validates that line items sum equals reported total

**Why**: Detects when LLM generates inconsistent data
- Example of hallucination: `LineItems = [$100, $200]` but `TotalAmount = $250`

**Implementation**:
```csharp
var lineItemsTotal = result.LineItems.Sum(item => item.Amount);
var difference = Math.Abs(result.TotalAmount - lineItemsTotal);
Assert.True(difference <= 0.01m, "Totals don't match");
```

**Tolerance**: ±0.01 (accounting for floating-point rounding)

**Test Cases Cover**:
- ✅ Standard invoice: 3 items + shipping = total
- ✅ Name variation: 3 service items = total
- ✅ Minimal: 2 items = total
- ✅ Precision: 4 items with decimals = $2000.00 total
- ✅ OCR variation: Various amounts sum correctly

### Evaluation B: Vendor Accuracy (Fuzzy Match)

**What**: Validates vendor name extraction using Levenshtein distance

**Why**: Real-world OCR and data entry create variations
- "ACME Corporation" vs "Acme Corp"
- "Tech Solutions" vs "Tech Solutionz" (typo)
- "CompuTech Solutions" vs "CompuTech Solut10ns" (OCR: 1→l)

**Implementation**:
```csharp
var distance = StringDistance.CalculateLevenshteinDistance(
    result.VendorName, 
    expectedVendor
);
Assert.True(distance <= 3, $"Distance {distance} > max 3");
```

**Threshold**: ≤ 3 character edits
- Handles typical OCR variations and typos
- Still strict enough to catch real mismatches

**Algorithm**: Levenshtein Distance
- **Edit Operations Allowed**: Insert, Delete, Replace
- **Case**: Insensitive (converted to lowercase)
- **Complexity**: O(n×m) where n, m = string lengths

**Example Calculations**:
```
Distance("ACME Corporation", "Acme Corp"):
- Replace 'C' with 'c': 1
- Replace 'O' with 'o': 1  
- Delete "oration": 7 ops
- Total: 9 (FAILS, but case-insensitive: 1 deletion of "oration" + context)

Distance("ACME Corp", "Acme Corp"):
- Case insensitive: 0 (PASSES ✓)

Distance("CompuTech Solutions", "CompuTech Solut10ns"):
- Replace 'i' with '1': 1 (OCR error)
- Total: 1 (PASSES ✓)
```

**Test Cases Cover**:
- ✅ Exact match: "ACME Corporation" = "ACME Corporation" (distance = 0)
- ✅ Case variation: "Acme Corp Inc." vs expected "Acme Corp Inc." (distance = 0)
- ✅ OCR errors: "CompuTech Solut10ns" has 1 character error (distance = 1)
- ✅ Within threshold: All variations ≤ 3 characters different

### Evaluation C: Date Validity (Format Validation)

**What**: Validates invoice date format and reasonableness

**Why**: Catches hallucinated dates and parsing errors
- Example of hallucination: Invoice dated in year 2099
- Example of parsing error: Invalid date format

**Implementation**:
```csharp
Assert.NotNull(result.InvoiceDate);
var today = DateTime.UtcNow.AddDays(1).Date;
Assert.True(result.InvoiceDate.Value.Date <= today, "Date in future");
Assert.True(result.InvoiceDate.Value.Year >= 2000, "Year unreasonably old");
```

**Validation Checks**:
1. ✅ Not null (field required)
2. ✅ Not in future (≤ today + 1 day tolerance)
3. ✅ Reasonable year (>= 2000)

**Test Cases Cover**:
- ✅ Standard date: 2024-11-15 (valid past date)
- ✅ Old date: 2024-10-20 (still in 2024)
- ✅ All test dates are today or recent past

## Mock Service Architecture

The tests use `MockInvoiceParser` to avoid API calls:

```csharp
internal class MockInvoiceParser : IInvoiceParser
{
    public Task<InvoiceExtractionResult> ExtractInvoiceAsync(...)
    {
        // Parses test invoice text
        // Extracts line items from $amount patterns
        // Extracts dates from YYYY-MM-DD format
        // Returns deterministic result
    }
}
```

**Advantages**:
- ✅ No API costs during testing
- ✅ Predictable, deterministic results
- ✅ Fast feedback loop
- ✅ Works offline

**To Switch to Real API**:
```csharp
private static IInvoiceParser CreateMockInvoiceService(...)
{
    // Replace with:
    return new OpenAIInvoiceService(apiKey);
    // Instead of:
    // return new MockInvoiceParser(...);
}
```

## Running Tests

### Run All Tests
```bash
dotnet test LLM-Integration.Tests/
```

**Output**:
```
Test run for c:\path\LLM-Integration.Tests.dll
Test Name: LLM_Integration.Tests.Evals.InvoiceExtractionEvals.Evaluate_InternalConsistency(invoiceText1, ...)
Test Outcome: Passed

Test Name: LLM_Integration.Tests.Evals.InvoiceExtractionEvals.Evaluate_VendorAccuracy(invoiceText1, ...)
Test Outcome: Passed

Test Name: LLM_Integration.Tests.Evals.InvoiceExtractionEvals.Evaluate_DateValidity(invoiceText1, ...)
Test Outcome: Passed
[... 12 more test cases ...]
```

### Run Specific Evaluation
```bash
dotnet test LLM-Integration.Tests/ --filter "Evaluate_VendorAccuracy"
```

### Run Specific Test Case
```bash
dotnet test LLM-Integration.Tests/ --filter "invoiceText1"
```

### Run with Verbose Output
```bash
dotnet test LLM-Integration.Tests/ -v d
```

## Extending the Golden Dataset

### Adding a New Test Case

1. **Add to `GetGoldenInvoices()`**:
```csharp
yield return new object[]
{
    // Your invoice text (raw)
    """
    INVOICE INV-2024-006
    Vendor: Your Company Name
    Date: 2024-11-20
    Items:
    - Service A: $500.00
    - Service B: $300.00
    Total: $800.00
    """,
    "Your Company Name",  // expected_vendor
    800.00m               // expected_total
};
```

2. **Automatic Result**: 
   - 3 new test cases automatically created
   - Runs: Consistency, Accuracy, Format validations
   - Total tests increase from 15 → 18

### Test Case Best Practices

**DO**:
- ✅ Include realistic invoice formats
- ✅ Vary vendor name formats (Inc., Corp., Ltd.)
- ✅ Test edge cases (max amounts, min amounts)
- ✅ Include OCR-like variations
- ✅ Document why the case is important

**DON'T**:
- ❌ Use meaningless data
- ❌ Mix inconsistent data (invoice says $100 but items = $200)
- ❌ Use future dates
- ❌ Use unrealistic amounts

## Customization

### Adjust Consistency Tolerance
```csharp
const decimal delta = 0.05m; // Was 0.01m, now allows ±0.05
```

### Adjust Vendor Match Threshold
```csharp
const int maxLevenshteinDistance = 5; // Was 3, now stricter
```

### Add Additional Validation

Example - Add Amount Validation:
```csharp
[Theory]
[MemberData(nameof(GetGoldenInvoices))]
public void Evaluate_AmountRange(string invoiceText, string expectedVendor, decimal expectedTotal)
{
    var result = mockService.ExtractInvoiceAsync(invoiceText).Result;
    
    // New validation: Amount should be between $0 and $1,000,000
    Assert.InRange(result.TotalAmount, 0, 1_000_000);
    foreach (var item in result.LineItems)
    {
        Assert.InRange(item.Amount, 0, 100_000);
    }
}
```

## Debugging Failed Tests

### Example Failure

```
Vendor name 'Acmme Corporation' is too different from expected 'ACME Corporation'. 
Levenshtein distance: 2 (max allowed: 3)
```

**Fix**: The extracted vendor has a typo "Acmme" (extra 'm'). This is a real LLM error.
- Option 1: Update test data to match actual extraction
- Option 2: Adjust threshold or improve prompt
- Option 3: Add to known issues/investigate LLM behavior

### Using Mock Debug

Modify `MockInvoiceParser` to print extracted values:
```csharp
Console.WriteLine($"Extracted Vendor: {_vendorName}");
Console.WriteLine($"Extracted Total: {_totalAmount}");
foreach (var item in lineItems)
{
    Console.WriteLine($"  - {item.Description}: {item.Amount}");
}
```

Then run with:
```bash
dotnet test LLM-Integration.Tests/ -v d 2>&1 | tee test-output.log
```

## Probabilistic Testing Principles

### 1. Golden Dataset
- Manually curated known-good outputs
- Regression testing: catches degradation
- Baseline for comparison

### 2. Multiple Evaluation Angles
- Consistency: Internal logic checks
- Accuracy: Against ground truth
- Format: Data quality checks

### 3. Tolerance-Based Assertions
- Not just exact matches
- Real-world considerations (rounding, OCR)
- Threshold-based: Levenshtein ≤ 3

### 4. Theory Tests
- Automatic parameterization
- Multiplies test coverage
- Easy to extend with new cases

## Integration with CI/CD

### GitHub Actions Example
```yaml
- name: Run Evals
  run: dotnet test LLM-Integration.Tests/ --verbosity normal
  
- name: Parse Test Results
  uses: actions/upload-artifact@v3
  with:
    name: test-results
    path: LLM-Integration.Tests/bin/Release/**/*.trx
```

### Quality Gates
- ✅ All 15 tests must pass
- ✅ Zero hallucinations (consistency check)
- ✅ Vendor accuracy ≤ 3 char distance
- ✅ All dates valid and reasonable

---

**Next**: Review `README.md` for production deployment guidance.
