using LLM_Integration.Services;
using System.Text.Json;

// Load API key from settings
var settings = JsonDocument.Parse(File.ReadAllText("Settings.json"));
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
    Console.WriteLine("Extracting invoice data...");
    var result = await invoiceService.ExtractInvoiceAsync(sampleInvoiceText);
    
    Console.WriteLine($"Invoice Number: {result.InvoiceNumber}");
    Console.WriteLine($"Vendor: {result.VendorName}");
    Console.WriteLine($"Date: {result.InvoiceDate:yyyy-MM-dd}");
    Console.WriteLine($"Total: ${result.TotalAmount:F2}");
    Console.WriteLine("\nLine Items:");
    
    foreach (var item in result.LineItems)
    {
        Console.WriteLine($"  - {item.Description}: ${item.Amount:F2}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
