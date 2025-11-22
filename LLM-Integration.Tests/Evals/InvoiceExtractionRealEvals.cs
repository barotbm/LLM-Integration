using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LLM_Integration.Models;
using LLM_Integration.Services;
using Xunit;
using Xunit.Sdk;

namespace LLM_Integration.Tests.Evals;

/// <summary>
/// End-to-end evaluation tests that call the real OpenAI model.
/// These tests are skipped automatically when no API key is present.
/// They exercise Accuracy, Consistency, Levenshtein similarity, Semantic correctness,
/// Model drift detection and Prompt effectiveness.
/// </summary>
public class InvoiceExtractionRealEvals
{
    private const int MaxLevenshteinDistance = 5; // allow more tolerance for real OCR-style input

    private static string? GetApiKey()
    {
        // Prefer environment variable for CI / local runs
        return Environment.GetEnvironmentVariable("OPENAI_API_KEY")
               ?? Environment.GetEnvironmentVariable("OPENAI_API")
               ?? Environment.GetEnvironmentVariable("OPENAI_KEY");
    }

    [Fact(DisplayName = "E2E: Invoice extraction full eval (real LLM)")]
    public async Task Full_Eval_Uses_Real_Model()
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new SkipException("Skipping E2E eval: OPENAI_API_KEY not configured in environment.");
        }

        // Use the same golden dataset as the unit tests
        var cases = InvoiceExtractionEvals.GetGoldenInvoices().ToList();

        var service = new OpenAIInvoiceService(apiKey);

        foreach (object[] data in cases)
        {
            var invoiceText = (string)data[0];
            var expectedVendor = (string)data[1];
            var expectedTotal = (decimal)data[2];

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var extraction = await service.ExtractInvoiceAsync(invoiceText, cts.Token);

            // Basic success checks
            Assert.NotNull(extraction);
            Assert.NotNull(extraction.Metrics);
            if (!extraction.Metrics.IsSuccessful)
            {
                // Surface the error from metrics when test fails
                Assert.True(false, $"Extraction failed: {extraction.Metrics.ErrorMessage}");
            }

            Assert.NotNull(extraction.Data);

            var result = extraction.Data!;

            // Accuracy: total amount should be close to expected (1% or 0.01 absolute)
            var accuracyDelta = Math.Max(0.01m, expectedTotal * 0.01m);
            Assert.True(
                Math.Abs(result.TotalAmount - expectedTotal) <= accuracyDelta,
                $"Accuracy failure. Expected total {expectedTotal:C}, got {result.TotalAmount:C} (allowed delta {accuracyDelta:C})");

            // Consistency: sum of line items should match total (allow small rounding delta)
            var lineItemsTotal = result.LineItems?.Sum(i => i.Amount) ?? 0m;
            var consistencyDelta = 0.05m;
            Assert.True(
                Math.Abs(lineItemsTotal - result.TotalAmount) <= consistencyDelta,
                $"Consistency failure. Line items sum {lineItemsTotal:C} does not match reported total {result.TotalAmount:C}");

            // Levenshtein similarity for vendor name
            Assert.False(string.IsNullOrWhiteSpace(result.VendorName), "VendorName must not be empty");
            var lev = StringDistance.CalculateLevenshteinDistance(result.VendorName!, expectedVendor);
            Assert.True(
                lev <= MaxLevenshteinDistance,
                $"Vendor name similarity failure. Extracted '{result.VendorName}' vs expected '{expectedVendor}'. Levenshtein: {lev}");

            // Semantic correctness: vendor token overlap heuristic + invoice date reasonable
            var expectedTokens = expectedVendor.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLowerInvariant()).ToArray();
            var extractedTokens = result.VendorName!.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLowerInvariant()).ToArray();
            var overlap = expectedTokens.Intersect(extractedTokens).Count();
            var overlapRatio = expectedTokens.Length == 0 ? 0 : (double)overlap / expectedTokens.Length;
            Assert.True(overlapRatio >= 0.33 || lev <= 2, $"Semantic vendor check failed. Overlap ratio {overlapRatio:F2}, Levenshtein {lev}");

            Assert.True(result.InvoiceDate.HasValue, "InvoiceDate must be present and parseable");
            Assert.True(result.InvoiceDate!.Value.Year >= 2000 && result.InvoiceDate.Value <= DateTime.UtcNow.AddDays(1),
                $"InvoiceDate seems invalid: {result.InvoiceDate:yyyy-MM-dd}");

            // Model drift: assert model name contains expected family (soft check)
            Assert.True(!string.IsNullOrWhiteSpace(extraction.Metrics.Model) && extraction.Metrics.Model!.ToLowerInvariant().Contains("gpt-4o"),
                $"Model drift warning: expected model family 'gpt-4o' but got '{extraction.Metrics.Model}'");

            // Prompt effectiveness: tokens and finish reason
            Assert.True(extraction.Metrics.TotalTokens > 0, "TotalTokens should be > 0 for a valid response");
            Assert.True(!string.IsNullOrWhiteSpace(extraction.Metrics.FinishReason) && extraction.Metrics.FinishReason != "length",
                $"Finish reason indicates truncation or unexpected termination: {extraction.Metrics.FinishReason}");

            // Log metrics for human inspection
            Console.WriteLine($"Case expectedVendor={expectedVendor}, expectedTotal={expectedTotal:C}");
            Console.WriteLine(extraction.Metrics.ToString());
        }
    }
}
