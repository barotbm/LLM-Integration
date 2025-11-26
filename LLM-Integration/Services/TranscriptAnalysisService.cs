using LLM_Integration.Models;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace LLM_Integration.Services;

/// <summary>
/// Service for analyzing mortgage servicing call transcripts using OpenAI API with GPT-4o.
/// Includes comprehensive observability metrics.
/// </summary>
public class TranscriptAnalysisService : ITranscriptAnalysisService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string Model = "gpt-4o-2024-08-06";
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";

    // Pricing per 1M tokens (as of November 2024)
    private const decimal InputTokenCostPer1M = 2.50m;
    private const decimal OutputTokenCostPer1M = 10.00m;

    // System prompt template with placeholders
    private const string SystemPromptTemplate = """
        ### 1. IDENTITY & ROLE
        You are {Role}, an AI assistant for {CompanyName}, a leading {Domain}.
        Your primary objective is to {Objective}.
        You must always prioritize accuracy, security, and regulatory compliance over creativity.

        ### 2. CONTEXT & KNOWLEDGE BASE
        You have access to the following context only:
        <context>
        {RetrievedContext}
        </context>

        - You must answer strictly using the information provided in <context>.
        - If the answer is not in the context, state: "I cannot answer this based on the available information."
        - Do not use outside knowledge or make assumptions about interest rates, approval odds, or legal statutes.

        ### 3. COMPLIANCE & GUARDRAILS (CRITICAL)
        You must adhere to the following non-negotiable rules:
        1. **No Financial Advice:** Never offer personalized investment advice or forecast market movements. Always add the disclaimer: "This is for informational purposes only."
        2. **PII Protection:** Never ask for or output full Social Security Numbers, Credit Card CVVs, or unmasked bank account numbers.
        3. **Regulatory Tone:** Avoid absolute guarantees (e.g., do not say "You will be approved," say "You may be eligible").
        4. **Fair Lending:** Do not consider or mention prohibited basis factors (Race, Color, Religion, National Origin, Sex, Marital Status, Age) in any decision logic.

        ### 4. TONE & VOICE
        - **Professional & Objective:** Use clear, concise, and business-appropriate language.
        - **Empathetic but Firm:** When discussing debts or denials, be respectful but direct. Do not use overly apologetic or emotional language.
        - **Avoid Jargon:** Explain financial acronyms (e.g., DTI, APR, LTV) if the user is a consumer.

        ### 5. REASONING PROCESS (Chain of Thought)
        Before answering, you must perform the following steps silently:
        1. Analyze the user's intent.
        2. Verify if the request violates any Compliance Guardrails.
        3. Search the <context> for evidence.
        4. Formulate the answer citing the specific document or policy section.

        ### 6. OUTPUT FORMAT
        {OutputInstructions}
        """;

    public TranscriptAnalysisService(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));
        }

        _apiKey = apiKey;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Analyzes a call transcript for compliance and quality using GPT-4o with structured outputs.
    /// </summary>
    public async Task<ExtractionResult<TranscriptAnalysisResult>> AnalyzeTranscriptAsync(
        string transcriptText, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transcriptText))
        {
            throw new ArgumentException("Transcript text cannot be null or empty.", nameof(transcriptText));
        }

        var overallStopwatch = Stopwatch.StartNew();
        var requestTimestampUtc = DateTime.UtcNow;
        var inputSizeBytes = Encoding.UTF8.GetByteCount(transcriptText);

        try
        {
            // Build the system prompt with actual values
            var systemPrompt = SystemPromptTemplate
                .Replace("{Role}", "Mortgage QA Analyst")
                .Replace("{CompanyName}", "FinTech Mortgage Solutions")
                .Replace("{Domain}", "Mortgage Servicing")
                .Replace("{Objective}", "audit agent-customer interactions for compliance and empathy")
                .Replace("{RetrievedContext}", transcriptText)
                .Replace("{OutputInstructions}", "Return a JSON object with a list of compliance issues, an overall score (1-10), and a summary.");

            var userMessage = """
                Analyze the transcript provided in the context above and return ONLY valid JSON with:
                - ComplianceIssues: Array of objects with Description (string) and Severity (string: "Low", "Medium", or "High")
                - OverallScore: Integer from 1-10
                - Summary: String summarizing the analysis
                """;

            var requestBody = new
            {
                model = Model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
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

            // Measure network request time
            var networkStopwatch = Stopwatch.StartNew();
            var response = await _httpClient.SendAsync(request, cancellationToken);
            networkStopwatch.Stop();

            var httpStatusCode = (int)response.StatusCode;

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                overallStopwatch.Stop();
                return CreateFailureResult<TranscriptAnalysisResult>(
                    requestTimestampUtc,
                    overallStopwatch.ElapsedMilliseconds,
                    networkStopwatch.ElapsedMilliseconds,
                    httpStatusCode,
                    inputSizeBytes,
                    ex.Message,
                    null);
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var responseSizeBytes = Encoding.UTF8.GetByteCount(jsonContent);

            var jsonDocument = JsonDocument.Parse(jsonContent);
            var root = jsonDocument.RootElement;

            // Extract metrics from response
            var usage = root.GetProperty("usage");
            var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
            var completionTokens = usage.GetProperty("completion_tokens").GetInt32();
            var totalTokens = usage.GetProperty("total_tokens").GetInt32();

            var finishReason = root.GetProperty("choices")[0]
                .GetProperty("finish_reason")
                .GetString() ?? "unknown";

            var responseContent = root.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            string? requestId = null;
            if (root.TryGetProperty("id", out var idElement))
            {
                requestId = idElement.GetString();
            }

            if (string.IsNullOrWhiteSpace(responseContent))
            {
                overallStopwatch.Stop();
                return CreateFailureResult<TranscriptAnalysisResult>(
                    requestTimestampUtc,
                    overallStopwatch.ElapsedMilliseconds,
                    networkStopwatch.ElapsedMilliseconds,
                    httpStatusCode,
                    inputSizeBytes,
                    "OpenAI API returned empty response.",
                    requestId);
            }

            var analysisResult = JsonSerializer.Deserialize<TranscriptAnalysisResult>(
                responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (analysisResult == null)
            {
                overallStopwatch.Stop();
                return CreateFailureResult<TranscriptAnalysisResult>(
                    requestTimestampUtc,
                    overallStopwatch.ElapsedMilliseconds,
                    networkStopwatch.ElapsedMilliseconds,
                    httpStatusCode,
                    inputSizeBytes,
                    "Failed to deserialize API response to TranscriptAnalysisResult.",
                    requestId);
            }

            overallStopwatch.Stop();

            // Calculate estimated cost
            var estimatedCost = CalculateEstimatedCost(promptTokens, completionTokens);

            // Calculate processing duration (total - network)
            var processingDuration = overallStopwatch.ElapsedMilliseconds - networkStopwatch.ElapsedMilliseconds;

            var metrics = new ExtractionMetrics
            {
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = totalTokens,
                RequestDurationMs = networkStopwatch.ElapsedMilliseconds,
                ProcessingDurationMs = processingDuration,
                TotalDurationMs = overallStopwatch.ElapsedMilliseconds,
                HttpStatusCode = httpStatusCode,
                Model = Model,
                RequestId = requestId,
                RequestTimestampUtc = requestTimestampUtc,
                EstimatedCostUsd = estimatedCost,
                InputSizeBytes = inputSizeBytes,
                ResponseSizeBytes = responseSizeBytes,
                IsSuccessful = true,
                ErrorMessage = null,
                FinishReason = finishReason
            };

            return new ExtractionResult<TranscriptAnalysisResult>
            {
                Data = analysisResult,
                Metrics = metrics
            };
        }
        catch (OperationCanceledException ex)
        {
            overallStopwatch.Stop();
            return CreateFailureResult<TranscriptAnalysisResult>(
                requestTimestampUtc,
                overallStopwatch.ElapsedMilliseconds,
                0,
                0,
                inputSizeBytes,
                $"Operation cancelled: {ex.Message}",
                null);
        }
        catch (Exception ex)
        {
            overallStopwatch.Stop();
            return CreateFailureResult<TranscriptAnalysisResult>(
                requestTimestampUtc,
                overallStopwatch.ElapsedMilliseconds,
                0,
                0,
                inputSizeBytes,
                $"Unexpected error: {ex.Message}",
                null);
        }
    }

    /// <summary>
    /// Creates a failure result with appropriate error metrics.
    /// </summary>
    private static ExtractionResult<T> CreateFailureResult<T>(
        DateTime requestTimestampUtc,
        long totalDurationMs,
        long networkDurationMs,
        int httpStatusCode,
        int inputSizeBytes,
        string errorMessage,
        string? requestId) where T : class
    {
        var metrics = new ExtractionMetrics
        {
            PromptTokens = 0,
            CompletionTokens = 0,
            TotalTokens = 0,
            RequestDurationMs = networkDurationMs,
            ProcessingDurationMs = totalDurationMs - networkDurationMs,
            TotalDurationMs = totalDurationMs,
            HttpStatusCode = httpStatusCode,
            Model = Model,
            RequestId = requestId,
            RequestTimestampUtc = requestTimestampUtc,
            EstimatedCostUsd = 0,
            InputSizeBytes = inputSizeBytes,
            ResponseSizeBytes = 0,
            IsSuccessful = false,
            ErrorMessage = errorMessage,
            FinishReason = "error"
        };

        return new ExtractionResult<T>
        {
            Data = null,
            Metrics = metrics
        };
    }

    /// <summary>
    /// Calculates the estimated cost of the API call based on token usage.
    /// </summary>
    private static decimal CalculateEstimatedCost(int promptTokens, int completionTokens)
    {
        var inputCost = (promptTokens / 1_000_000m) * InputTokenCostPer1M;
        var outputCost = (completionTokens / 1_000_000m) * OutputTokenCostPer1M;
        return inputCost + outputCost;
    }
}
