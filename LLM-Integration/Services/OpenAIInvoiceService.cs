using LLM_Integration.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace LLM_Integration.Services;

/// <summary>
/// Service for extracting invoice data using the OpenAI API with GPT-4o and Structured Outputs.
/// </summary>
public class OpenAIInvoiceService : IInvoiceParser
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string SystemPrompt = "You are a financial data extraction assistant. Extract data strictly. If a field is missing, return null.";
    private const string Model = "gpt-4o-2024-08-06";
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

    public OpenAIInvoiceService(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
        }

        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Extracts invoice data from raw invoice text using GPT-4o with Structured Outputs.
    /// </summary>
    /// <param name="invoiceText">Raw text content of the invoice.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Extracted invoice data as an InvoiceExtractionResult.</returns>
    public async Task<InvoiceExtractionResult> ExtractInvoiceAsync(string invoiceText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(invoiceText))
        {
            throw new ArgumentException("Invoice text cannot be null or empty.", nameof(invoiceText));
        }

        var userMessage = $"""
            Extract the following information from this invoice text and return ONLY valid JSON:
            - InvoiceNumber (string or null)
            - VendorName (string or null)
            - InvoiceDate (ISO 8601 format string or null)
            - TotalAmount (decimal number)
            - LineItems (array of objects with Description and Amount)

            Invoice text:
            {invoiceText}
            """;

        var requestBody = new
        {
            model = Model,
            messages = new object[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = userMessage }
            },
            response_format = new { type = "json_object" },
            temperature = 0
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = JsonContent.Create(requestBody)
        };

        request.Headers.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var jsonDocument = JsonDocument.Parse(jsonContent);
        var root = jsonDocument.RootElement;
        var responseContent = root.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(responseContent))
        {
            throw new InvalidOperationException("OpenAI API returned empty response.");
        }

        var result = JsonSerializer.Deserialize<InvoiceExtractionResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize API response to InvoiceExtractionResult.");
        }

        return result;
    }
}
