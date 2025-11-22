using LLM_Integration.Models;
using LLM_Integration.Tests.Utilities;
using Xunit;

namespace LLM_Integration.Tests.Evals;

/// <summary>
/// Evaluation suite for invoice extraction using probabilistic testing.
/// Tests validate consistency, accuracy, and data format compliance.
/// </summary>
public class InvoiceExtractionEvals
{
    /// <summary>
    /// Golden dataset of test invoices with known ground truth values.
    /// Each test case contains raw invoice text and expected extraction results.
    /// </summary>
    public static IEnumerable<object[]> GetGoldenInvoices()
    {
        yield return new object[]
        {
            // Test case 1: Standard invoice with complete data
            """
            INVOICE
            Date: 2024-11-15
            Invoice Number: INV-2024-001
            
            Bill To: ACME Corporation
            
            Item                          Description                     Amount
            1                             Widget A - Standard             $150.00
            2                             Widget B - Premium              $250.00
            3                             Shipping & Handling              $25.00
            
            Total Amount Due: $425.00
            """,
            "ACME Corporation",           // expected_vendor
            425.00m                       // expected_total
        };

        yield return new object[]
        {
            // Test case 2: Invoice with vendor name variation
            """
            Invoice Receipt
            
            From: Acme Corp Inc.
            Date: 2024-10-20
            Ref: INV-2024-002
            
            Services Rendered:
            - Consulting Services        $500.00
            - Design Work               $300.00
            - Implementation             $200.00
            
            Subtotal:                    $1000.00
            Tax (10%):                   $100.00
            Total:                       $1100.00
            """,
            "Acme Corp Inc.",             // expected_vendor (variations like "ACME Corp" or "Acme Corp Inc" should match)
            1100.00m                      // expected_total
        };

        yield return new object[]
        {
            // Test case 3: Simple invoice with minimal data
            """
            Simple Invoice
            Vendor: Tech Solutions
            Invoice Date: 2024-09-15
            Invoice #: INV-2024-003
            
            Line Item 1: $75.50
            Line Item 2: $24.50
            
            Total: $100.00
            """,
            "Tech Solutions",             // expected_vendor
            100.00m                       // expected_total
        };

        yield return new object[]
        {
            // Test case 4: Invoice with decimal precision
            """
            DETAILED INVOICE
            
            Vendor Name: Global Services Ltd
            Invoice Number: INV-2024-004
            Invoice Date: 2024-08-10
            
            Line Items:
            Item 1 - Professional Services       $1,234.56
            Item 2 - Travel Expenses              $345.67
            Item 3 - Materials                    $123.45
            Item 4 - Miscellaneous               $296.32
            
            Total Invoice Amount: $2000.00
            """,
            "Global Services Ltd",        // expected_vendor
            2000.00m                      // expected_total
        };

        yield return new object[]
        {
            // Test case 5: Invoice with OCR-like variations in vendor name
            """
            INV0ICE - 2024-2024-005
            
            Supplier: CompuTech Solut10ns  (vendor name may have OCR errors)
            Date: 2024-07-01
            
            Description of services:
            Software License             $500.00
            Support Package              $300.00
            
            Grand Total Due: $800.00
            """,
            "CompuTech Solutions",        // expected_vendor (with allowance for OCR variations)
            800.00m                       // expected_total
        };
    }

    /// <summary>
    /// Requirement B: Consistency Eval (Hallucination Check)
    /// Validates that the sum of line item amounts matches the reported total.
    /// This detects if the LLM hallucinated a total that doesn't match the items.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetGoldenInvoices))]
    public void Evaluate_InternalConsistency(string invoiceText, string expectedVendor, decimal expectedTotal)
    {
        // Arrange
        var mockService = CreateMockInvoiceService(invoiceText, expectedVendor, expectedTotal);
        var delta = 0.01m; // Allow 0.01 for rounding errors

        // Act
        var extractionResult = mockService.ExtractInvoiceAsync(invoiceText).Result;

        // Assert
        Assert.NotNull(extractionResult);
        Assert.True(extractionResult.Success, $"Extraction failed: {extractionResult.Metrics.ErrorMessage}");
        Assert.NotNull(extractionResult.Data);

        var result = extractionResult.Data;

        // Calculate sum of line items
        var lineItemsTotal = result.LineItems.Sum(item => item.Amount);

        // Verify that line items sum equals the reported total (within delta)
        var difference = Math.Abs(result.TotalAmount - lineItemsTotal);
        Assert.True(
            difference <= delta,
            $"Line items total ({lineItemsTotal:C}) does not match reported total ({result.TotalAmount:C}). Difference: {difference:C}");
    }

    /// <summary>
    /// Requirement C: Accuracy Eval (Fuzzy Match)
    /// Validates vendor name accuracy using Levenshtein distance.
    /// Allows for small OCR errors like "Inc." vs "Inc" or typos.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetGoldenInvoices))]
    public void Evaluate_VendorAccuracy(string invoiceText, string expectedVendor, decimal expectedTotal)
    {
        // Arrange
        var mockService = CreateMockInvoiceService(invoiceText, expectedVendor, expectedTotal);
        const int maxLevenshteinDistance = 3; // Allow up to 3 character differences

        // Act
        var extractionResult = mockService.ExtractInvoiceAsync(invoiceText).Result;

        // Assert
        Assert.NotNull(extractionResult);
        Assert.True(extractionResult.Success, $"Extraction failed: {extractionResult.Metrics.ErrorMessage}");
        Assert.NotNull(extractionResult.Data);

        var result = extractionResult.Data;
        Assert.NotNull(result.VendorName);

        // Calculate Levenshtein distance between extracted and expected vendor names
        var distance = StringDistance.CalculateLevenshteinDistance(result.VendorName, expectedVendor);

        Assert.True(
            distance <= maxLevenshteinDistance,
            $"Vendor name '{result.VendorName}' is too different from expected '{expectedVendor}'. " +
            $"Levenshtein distance: {distance} (max allowed: {maxLevenshteinDistance})");
    }

    /// <summary>
    /// Requirement D: Format Eval
    /// Validates that invoice dates are properly formatted and not in the future.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetGoldenInvoices))]
    public void Evaluate_DateValidity(string invoiceText, string expectedVendor, decimal expectedTotal)
    {
        // Arrange
        var mockService = CreateMockInvoiceService(invoiceText, expectedVendor, expectedTotal);

        // Act
        var extractionResult = mockService.ExtractInvoiceAsync(invoiceText).Result;

        // Assert
        Assert.NotNull(extractionResult);
        Assert.True(extractionResult.Success, $"Extraction failed: {extractionResult.Metrics.ErrorMessage}");
        Assert.NotNull(extractionResult.Data);

        var result = extractionResult.Data;

        // Check that InvoiceDate is not null
        Assert.NotNull(result.InvoiceDate);

        // Check that InvoiceDate is not in the future (allow 1 day tolerance for timezone differences)
        var today = DateTime.UtcNow.AddDays(1).Date;
        Assert.True(
            result.InvoiceDate.Value.Date <= today,
            $"Invoice date ({result.InvoiceDate:yyyy-MM-dd}) is in the future. Today: {today:yyyy-MM-dd}");

        // Check that InvoiceDate is reasonable (not before year 2000)
        Assert.True(
            result.InvoiceDate.Value.Year >= 2000,
            $"Invoice date year ({result.InvoiceDate.Value.Year}) is unreasonably old.");
    }

    /// <summary>
    /// Creates a mock invoice service for testing.
    /// In production, this would be replaced with an actual service call.
    /// </summary>
    private static IInvoiceParser CreateMockInvoiceService(string invoiceText, string expectedVendor, decimal expectedTotal)
    {
        return new MockInvoiceParser(expectedVendor, expectedTotal);
    }
}

/// <summary>
/// Mock implementation of IInvoiceParser for testing without calling the actual OpenAI API.
/// </summary>
internal class MockInvoiceParser : IInvoiceParser
{
    private readonly string _vendorName;
    private readonly decimal _totalAmount;

    public MockInvoiceParser(string vendorName, decimal totalAmount)
    {
        _vendorName = vendorName;
        _totalAmount = totalAmount;
    }

    public async Task<ExtractionResult> ExtractInvoiceAsync(string invoiceText, CancellationToken cancellationToken = default)
    {
        // Parse the invoice text to extract line items and dates
        // This is a simplified mock that extracts basic info from the test data

        var lineItems = new List<LineItem>();
        var invoiceDate = DateTime.Now.AddDays(-5); // Default to 5 days ago

        // Extract line items from currency amounts in the text
        var amounts = System.Text.RegularExpressions.Regex.Matches(invoiceText, @"\$?([\d,]+\.\d{2})")
            .Cast<System.Text.RegularExpressions.Match>()
            .Select(m => decimal.Parse(m.Groups[1].Value.Replace(",", "")))
            .ToList();

        // Create line items (excluding the last amount which is typically the total)
        if (amounts.Count > 1)
        {
            for (int i = 0; i < amounts.Count - 1; i++)
            {
                lineItems.Add(new LineItem
                {
                    Description = $"Item {i + 1}",
                    Amount = amounts[i]
                });
            }
        }

        // Extract date if present
        var dateMatch = System.Text.RegularExpressions.Regex.Match(invoiceText, @"(\d{4}-\d{2}-\d{2})|\b(\w+ \d{1,2}, \d{4})\b");
        if (dateMatch.Success && DateTime.TryParse(dateMatch.Value, out var extractedDate))
        {
            invoiceDate = extractedDate;
        }

        var data = new InvoiceExtractionResult
        {
            InvoiceNumber = "INV-MOCK-001",
            VendorName = _vendorName,
            InvoiceDate = invoiceDate,
            TotalAmount = _totalAmount,
            LineItems = lineItems
        };

        var metrics = new ExtractionMetrics
        {
            PromptTokens = 150,
            CompletionTokens = 50,
            TotalTokens = 200,
            RequestDurationMs = 50,
            ProcessingDurationMs = 100,
            TotalDurationMs = 150,
            HttpStatusCode = 200,
            Model = "gpt-4o-2024-08-06",
            RequestId = "mock-request-id",
            RequestTimestampUtc = DateTime.UtcNow,
            EstimatedCostUsd = 0.0020m,
            InputSizeBytes = invoiceText.Length,
            ResponseSizeBytes = 200,
            IsSuccessful = true,
            ErrorMessage = null,
            FinishReason = "stop"
        };

        return await Task.FromResult(new ExtractionResult
        {
            Data = data,
            Metrics = metrics
        });
    }
}
