using LLM_Integration.Services;
using System.Text.Json;

// Load API key from settings
var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Settings.json")));
var apiKey = settings.RootElement.GetProperty("API-Key").GetString();

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("Error: API key not configured in Settings.json");
    return;
}

// Initialize the invoice extraction service
var invoiceService = new OpenAIInvoiceService(apiKey);

// Example: Extract invoice data
var sampleInvoiceText = """
    INVOICE
    Invoice Number: INV-2024-001
    Vendor: ACME Corporation
    Invoice Date: 2024-11-15
    
    Items:
    - Widget A: $150.00
    - Widget B: $250.00
    - Shipping: $25.00
    
    Total: $425.00
    """;

try
{
    Console.WriteLine("Extracting invoice data...\n");
    var result = await invoiceService.ExtractInvoiceAsync(sampleInvoiceText);
    
    if (result.Success && result.Data != null)
    {
        Console.WriteLine("=== EXTRACTION RESULTS ===");
        Console.WriteLine($"Invoice Number: {result.Data.InvoiceNumber}");
        Console.WriteLine($"Vendor: {result.Data.VendorName}");
        Console.WriteLine($"Date: {result.Data.InvoiceDate:yyyy-MM-dd}");
        Console.WriteLine($"Total: ${result.Data.TotalAmount:F2}");
        Console.WriteLine("\nLine Items:");
        
        foreach (var item in result.Data.LineItems)
        {
            Console.WriteLine($"  - {item.Description}: ${item.Amount:F2}");
        }
    }
    else
    {
        Console.WriteLine("=== EXTRACTION FAILED ===");
        Console.WriteLine($"Error: {result.Metrics.ErrorMessage}");
    }

    Console.WriteLine("\n=== OBSERVABILITY METRICS ===");
    Console.WriteLine(result.Metrics);

    Console.WriteLine("\n\n=== DETAILED METRICS BREAKDOWN ===");
    Console.WriteLine($"HTTP Status Code:        {result.Metrics.HttpStatusCode}");
    Console.WriteLine($"Total Tokens:            {result.Metrics.TotalTokens:N0} (prompt: {result.Metrics.PromptTokens:N0}, completion: {result.Metrics.CompletionTokens:N0})");
    Console.WriteLine($"Total Duration:          {result.Metrics.TotalDurationMs}ms");
    Console.WriteLine($"  - Network Time:       {result.Metrics.RequestDurationMs}ms");
    Console.WriteLine($"  - Processing Time:    {result.Metrics.ProcessingDurationMs}ms");
    Console.WriteLine($"Finish Reason:           {result.Metrics.FinishReason}");
    Console.WriteLine($"Estimated Cost:          ${result.Metrics.EstimatedCostUsd:F8}");
    Console.WriteLine($"Request ID:              {result.Metrics.RequestId ?? "N/A"}");
    Console.WriteLine($"Input Size:              {result.Metrics.InputSizeBytes} bytes");
    Console.WriteLine($"Response Size:           {result.Metrics.ResponseSizeBytes} bytes");
    Console.WriteLine($"Request Timestamp:       {result.Metrics.RequestTimestampUtc:yyyy-MM-dd HH:mm:ss.fff} UTC");

    Console.WriteLine("\n\nPress any key to exit...");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
