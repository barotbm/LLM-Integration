namespace LLM_Integration.Models;

/// <summary>
/// Represents the structured result of transcript analysis.
/// </summary>
public record TranscriptAnalysisResult
{
    /// <summary>
    /// List of compliance issues identified in the transcript.
    /// </summary>
    public List<ComplianceIssue> ComplianceIssues { get; init; } = new();

    /// <summary>
    /// Overall quality score from 1-10.
    /// </summary>
    public int OverallScore { get; init; }

    /// <summary>
    /// Summary of the transcript analysis.
    /// </summary>
    public string? Summary { get; init; }
}

/// <summary>
/// Represents a single compliance issue found in the transcript.
/// </summary>
public record ComplianceIssue
{
    /// <summary>
    /// Description of the compliance issue.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Severity level of the issue (e.g., "Low", "Medium", "High").
    /// </summary>
    public required string Severity { get; init; }
}
