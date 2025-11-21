namespace LLM_Integration.Tests.Utilities;

/// <summary>
/// String utility functions for evaluation metrics.
/// </summary>
public static class StringDistance
{
    /// <summary>
    /// Calculates the Levenshtein distance between two strings.
    /// The Levenshtein distance is the minimum number of single-character edits 
    /// (insertions, deletions, or substitutions) required to change one string into another.
    /// </summary>
    /// <param name="source">The source string.</param>
    /// <param name="target">The target string.</param>
    /// <returns>The Levenshtein distance between the two strings.</returns>
    public static int CalculateLevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source))
        {
            return string.IsNullOrEmpty(target) ? 0 : target.Length;
        }

        if (string.IsNullOrEmpty(target))
        {
            return source.Length;
        }

        // Convert to lowercase for case-insensitive comparison
        source = source.ToLowerInvariant();
        target = target.ToLowerInvariant();

        var sourceLength = source.Length;
        var targetLength = target.Length;

        // Create a 2D array to store distances
        var distances = new int[sourceLength + 1, targetLength + 1];

        // Initialize the first column and row
        for (var i = 0; i <= sourceLength; i++)
        {
            distances[i, 0] = i;
        }

        for (var j = 0; j <= targetLength; j++)
        {
            distances[0, j] = j;
        }

        // Calculate distances
        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= targetLength; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;

                distances[i, j] = Math.Min(
                    Math.Min(
                        distances[i - 1, j] + 1,        // Deletion
                        distances[i, j - 1] + 1),       // Insertion
                    distances[i - 1, j - 1] + cost);    // Substitution
            }
        }

        return distances[sourceLength, targetLength];
    }
}
