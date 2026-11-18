using CmSecurityService.Incidents;

namespace CmSecurityService.Tests;

/// <summary>
/// Tests for the query-string validation of GET /incidents (acceptance criteria 1 and 2).
/// </summary>
public class IncidentQueryTests
{
    [Fact]
    public void Validate_DefaultQuery_HasNoErrors()
    {
        var query = new IncidentQuery();

        Assert.Empty(query.Validate());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_PageBelowOne_ReturnsError(int page)
    {
        var query = new IncidentQuery { Page = page };

        var errors = query.Validate();

        Assert.Contains(errors, e => e.Contains("page must be 1 or greater"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(IncidentQuery.MaxPageSize + 1)]
    public void Validate_PageSizeOutOfRange_ReturnsError(int pageSize)
    {
        var query = new IncidentQuery { PageSize = pageSize };

        var errors = query.Validate();

        Assert.Contains(errors, e => e.Contains("pageSize must be between 1 and"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(IncidentQuery.DefaultPageSize)]
    [InlineData(IncidentQuery.MaxPageSize)]
    public void Validate_PageSizeInRange_HasNoErrors(int pageSize)
    {
        var query = new IncidentQuery { PageSize = pageSize };

        Assert.Empty(query.Validate());
    }

    [Fact]
    public void Validate_FromAfterTo_ReturnsError()
    {
        var query = new IncidentQuery
        {
            From = new DateTime(2026, 10, 20),
            To = new DateTime(2026, 10, 10)
        };

        var errors = query.Validate();

        Assert.Contains(errors, e => e.Contains("from must be earlier than or equal to to"));
    }

    [Fact]
    public void Validate_FromEqualToTo_HasNoErrors()
    {
        var day = new DateTime(2026, 10, 15);
        var query = new IncidentQuery { From = day, To = day };

        Assert.Empty(query.Validate());
    }

    [Fact]
    public void Validate_MultipleProblems_ReturnsAllErrors()
    {
        var query = new IncidentQuery
        {
            Page = 0,
            PageSize = 500,
            From = new DateTime(2026, 10, 20),
            To = new DateTime(2026, 10, 10)
        };

        Assert.Equal(3, query.Validate().Count);
    }

    [Fact]
    public void Validate_StatusText_IsNotCheckedHere()
    {
        // Status names live in the database, so the service checks them (see
        // EfIncidentQueryServiceTests), not this class.
        var query = new IncidentQuery { Status = "Banana" };

        Assert.Empty(query.Validate());
    }
}