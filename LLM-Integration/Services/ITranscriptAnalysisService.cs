using LLM_Integration.Models;

namespace LLM_Integration.Services;

/// <summary>
/// Interface for analyzing mortgage servicing call transcripts for compliance and quality.
/// </summary>
public interface ITranscriptAnalysisService
{
    /// <summary>
    /// Analyzes a call transcript for compliance issues and quality metrics.
    /// </summary>
    /// <param name="transcriptText">The raw transcript text to analyze.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Analysis result containing compliance issues, score, and summary with metrics.</returns>
    Task<ExtractionResult<TranscriptAnalysisResult>> AnalyzeTranscriptAsync(
        string transcriptText, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Extension methods for transcript analysis.
/// </summary>
public static class TranscriptAnalysisServiceExtensions
{
    /// <summary>
    /// Analyzes a transcript without returning metrics (legacy/simple usage).
    /// </summary>
    /// <param name="service">The transcript analysis service.</param>
    /// <param name="transcriptText">The raw transcript text to analyze.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Analysis result or null if analysis failed.</returns>
    public static async Task<TranscriptAnalysisResult?> AnalyzeTranscriptSimpleAsync(
        this ITranscriptAnalysisService service,
        string transcriptText,
        CancellationToken cancellationToken = default)
    {
        var result = await service.AnalyzeTranscriptAsync(transcriptText, cancellationToken);
        return result.Data;
    }
}
