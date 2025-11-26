using LLM_Integration.Services;
using System.Text.Json;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║         LLM Integration Demo - Multi-Service Platform         ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

// Load API key from settings
var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Settings.json")));
var apiKey = settings.RootElement.GetProperty("API-Key").GetString();

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("Error: API key not configured in Settings.json");
    return;
}

// Initialize both services
var invoiceService = new OpenAIInvoiceService(apiKey);
var transcriptService = new TranscriptAnalysisService(apiKey);

// Menu selection
Console.WriteLine("Select a demo to run:");
Console.WriteLine("1. Invoice Extraction");
Console.WriteLine("2. Transcript Analysis");
Console.WriteLine("3. Both\n");
Console.Write("Enter your choice (1-3): ");
var choice = Console.ReadLine();

var runInvoice = choice == "1" || choice == "3";
var runTranscript = choice == "2" || choice == "3";

if (!runInvoice && !runTranscript)
{
    Console.WriteLine("Invalid choice. Exiting.");
    return;
}

try
{
    // ==================== INVOICE EXTRACTION DEMO ====================
    if (runInvoice)
    {
        Console.WriteLine("\n\n╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                    INVOICE EXTRACTION DEMO                     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

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

        Console.WriteLine("Extracting invoice data...\n");
        var invoiceResult = await invoiceService.ExtractInvoiceAsync(sampleInvoiceText);
        
        if (invoiceResult.Success && invoiceResult.Data != null)
        {
            Console.WriteLine("✓ EXTRACTION SUCCESSFUL\n");
            Console.WriteLine($"Invoice Number: {invoiceResult.Data.InvoiceNumber}");
            Console.WriteLine($"Vendor:         {invoiceResult.Data.VendorName}");
            Console.WriteLine($"Date:           {invoiceResult.Data.InvoiceDate:yyyy-MM-dd}");
            Console.WriteLine($"Total:          ${invoiceResult.Data.TotalAmount:F2}");
            Console.WriteLine("\nLine Items:");
            
            foreach (var item in invoiceResult.Data.LineItems)
            {
                Console.WriteLine($"  • {item.Description}: ${item.Amount:F2}");
            }
        }
        else
        {
            Console.WriteLine("✗ EXTRACTION FAILED");
            Console.WriteLine($"Error: {invoiceResult.Metrics.ErrorMessage}");
        }

        Console.WriteLine("\n--- Observability Metrics ---");
        Console.WriteLine($"Tokens:     {invoiceResult.Metrics.TotalTokens:N0} (prompt: {invoiceResult.Metrics.PromptTokens:N0}, completion: {invoiceResult.Metrics.CompletionTokens:N0})");
        Console.WriteLine($"Duration:   {invoiceResult.Metrics.TotalDurationMs}ms (network: {invoiceResult.Metrics.RequestDurationMs}ms)");
        Console.WriteLine($"Cost:       ${invoiceResult.Metrics.EstimatedCostUsd:F6}");
        Console.WriteLine($"Request ID: {invoiceResult.Metrics.RequestId ?? "N/A"}");
    }

    // ==================== TRANSCRIPT ANALYSIS DEMO ====================
    if (runTranscript)
    {
        Console.WriteLine("\n\n╔════════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                   TRANSCRIPT ANALYSIS DEMO                     ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

        var sampleTranscript = """
            Agent: Good morning! This is Sarah from FinTech Mortgage Solutions. How can I help you today?
            
            Customer: Hi Sarah. I'm calling because I'm having some trouble making my mortgage payment this month. I lost my job last week and I'm not sure what to do.
            
            Agent: I'm sorry to hear that. Let me help you explore your options. First, can I get your loan number so I can pull up your account?
            
            Customer: Sure, it's 123456789.
            
            Agent: Thank you. I see your account here. Given your situation, you may be eligible for a forbearance program. This would allow you to temporarily reduce or suspend your payments while you get back on your feet. Would you like to hear more about this option?
            
            Customer: Yes, that sounds helpful. How does it work?
            
            Agent: With forbearance, you can reduce your payment for up to 6 months. After that period, we'll work with you to create a repayment plan. This won't negatively impact your credit score. I want to be clear though - this is for informational purposes only, and we'll need to review your full financial situation to determine your eligibility.
            
            Customer: That makes sense. What do I need to do to apply?
            
            Agent: I can start the application process right now over the phone. I'll need some information about your current financial situation, but I won't ask for sensitive details like your full Social Security Number over the phone for security reasons. We'll send you a secure link to complete the application.
            
            Customer: Okay, that sounds good. I appreciate your help.
            
            Agent: You're welcome. Remember, there are options available, and we're here to help you through this difficult time. Is there anything else I can assist you with today?
            
            Customer: No, that's all. Thank you.
            
            Agent: You're welcome. Have a great day, and we'll be in touch soon about your forbearance application.
            """;

        Console.WriteLine("Analyzing transcript for compliance and quality...\n");
        var transcriptResult = await transcriptService.AnalyzeTranscriptAsync(sampleTranscript);
        
        if (transcriptResult.Success && transcriptResult.Data != null)
        {
            Console.WriteLine("✓ ANALYSIS COMPLETE\n");
            Console.WriteLine($"Overall Score: {transcriptResult.Data.OverallScore}/10");
            Console.WriteLine($"\nSummary:\n{transcriptResult.Data.Summary}");
            
            if (transcriptResult.Data.ComplianceIssues.Count > 0)
            {
                Console.WriteLine($"\n⚠ Compliance Issues Found: {transcriptResult.Data.ComplianceIssues.Count}");
                foreach (var issue in transcriptResult.Data.ComplianceIssues)
                {
                    var severityIcon = issue.Severity switch
                    {
                        "High" => "🔴",
                        "Medium" => "🟡",
                        "Low" => "🟢",
                        _ => "⚪"
                    };
                    Console.WriteLine($"  {severityIcon} [{issue.Severity}] {issue.Description}");
                }
            }
            else
            {
                Console.WriteLine("\n✓ No compliance issues detected");
            }
        }
        else
        {
            Console.WriteLine("✗ ANALYSIS FAILED");
            Console.WriteLine($"Error: {transcriptResult.Metrics.ErrorMessage}");
        }

        Console.WriteLine("\n--- Observability Metrics ---");
        Console.WriteLine($"Tokens:     {transcriptResult.Metrics.TotalTokens:N0} (prompt: {transcriptResult.Metrics.PromptTokens:N0}, completion: {transcriptResult.Metrics.CompletionTokens:N0})");
        Console.WriteLine($"Duration:   {transcriptResult.Metrics.TotalDurationMs}ms (network: {transcriptResult.Metrics.RequestDurationMs}ms)");
        Console.WriteLine($"Cost:       ${transcriptResult.Metrics.EstimatedCostUsd:F6}");
        Console.WriteLine($"Request ID: {transcriptResult.Metrics.RequestId ?? "N/A"}");
    }

    Console.WriteLine("\n\n════════════════════════════════════════════════════════════════");
    Console.WriteLine("Demo completed successfully!");
    Console.WriteLine("════════════════════════════════════════════════════════════════");
    Console.WriteLine("\nPress any key to exit...");
    Console.ReadLine();
}
catch (Exception ex)
{
    Console.WriteLine($"\n✗ Error: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
}
