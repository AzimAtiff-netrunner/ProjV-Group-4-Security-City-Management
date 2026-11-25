namespace CmSecurityService.Suspects;

/// <summary>
/// One row of GET /api/security/v1/neighborhoods/crime-rankings (story #225).
/// </summary>
public class NeighborhoodRankingDto
{
    /// <summary>Position in the ranking, 1 = most incidents. Neighborhoods with equal counts share a rank.</summary>
    public int Rank { get; set; }

    /// <summary>Neighborhood name, taken from the incident location.</summary>
    public string Neighborhood { get; set; } = string.Empty;

    /// <summary>Number of incidents reported in this neighborhood for the chosen dates.</summary>
    public int IncidentCount { get; set; }
}

/// <summary>
/// Options for GET /api/security/v1/neighborhoods/crime-rankings.
/// </summary>
public class CrimeRankingQuery
{
    /// <summary>Default number of neighborhoods returned.</summary>
    public const int DefaultTop = 10;

    /// <summary>Largest number of neighborhoods a caller may request.</summary>
    public const int MaxTop = 50;

    /// <summary>Optional start of the reported date range (inclusive).</summary>
    public DateTime? From { get; set; }

    /// <summary>Optional end of the reported date range (inclusive; a date with no time means the whole day).</summary>
    public DateTime? To { get; set; }

    /// <summary>How many neighborhoods to return (1 to <see cref="MaxTop"/>).</summary>
    public int Top { get; set; } = DefaultTop;

    /// <summary>
    /// Checks the query for invalid values.
    /// </summary>
    /// <returns>Human-readable error messages. Empty when the query is valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (Top < 1 || Top > MaxTop)
        {
            errors.Add($"top must be between 1 and {MaxTop}.");
        }

        if (From.HasValue && To.HasValue && From.Value > To.Value)
        {
            errors.Add("from must be earlier than or equal to to.");
        }

        return errors;
    }
}
