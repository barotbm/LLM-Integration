using LLM_Integration.Models;
using LLM_Integration.Services;
using Xunit;

namespace LLM_Integration.Tests.Services;

/// <summary>
/// Unit tests for TranscriptAnalysisService.
/// Uses mock data to avoid calling the real OpenAI API during testing.
/// </summary>
public class TranscriptAnalysisServiceTests
{
    [Fact]
    public void Constructor_ThrowsException_WhenApiKeyIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TranscriptAnalysisService(null!));
    }

    [Fact]
    public void Constructor_ThrowsException_WhenApiKeyIsEmpty()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TranscriptAnalysisService(string.Empty));
    }

    [Fact]
    public void Constructor_ThrowsException_WhenApiKeyIsWhitespace()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TranscriptAnalysisService("   "));
    }

    [Fact]
    public async Task AnalyzeTranscriptAsync_ThrowsException_WhenTranscriptIsNull()
    {
        // Arrange
        var service = new TranscriptAnalysisService("test-key");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.AnalyzeTranscriptAsync(null!));
    }

    [Fact]
    public async Task AnalyzeTranscriptAsync_ThrowsException_WhenTranscriptIsEmpty()
    {
        // Arrange
        var service = new TranscriptAnalysisService("test-key");

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            service.AnalyzeTranscriptAsync(string.Empty));
    }

    [Fact]
    public void TranscriptAnalysisResult_HasCorrectProperties()
    {
        // Arrange & Act
        var result = new TranscriptAnalysisResult
        {
            OverallScore = 8,
            Summary = "Good call quality",
            ComplianceIssues = new List<ComplianceIssue>
            {
                new ComplianceIssue
                {
                    Description = "Test issue",
                    Severity = "Low"
                }
            }
        };

        // Assert
        Assert.Equal(8, result.OverallScore);
        Assert.Equal("Good call quality", result.Summary);
        Assert.Single(result.ComplianceIssues);
        Assert.Equal("Test issue", result.ComplianceIssues[0].Description);
        Assert.Equal("Low", result.ComplianceIssues[0].Severity);
    }

    [Fact]
    public void ComplianceIssue_RequiresDescriptionAndSeverity()
    {
        // Arrange & Act
        var issue = new ComplianceIssue
        {
            Description = "Missing disclaimer",
            Severity = "Medium"
        };

        // Assert
        Assert.NotNull(issue.Description);
        Assert.NotNull(issue.Severity);
        Assert.Equal("Missing disclaimer", issue.Description);
        Assert.Equal("Medium", issue.Severity);
    }
}
