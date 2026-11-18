using System.ComponentModel.DataAnnotations;
using System.Reflection;
using CmSecurityService.Controllers;

namespace CmSecurityService.Tests;

/// <summary>
/// Tests for the validation rules on the POST /incidents body (acceptance criteria 1 and 2).
/// </summary>
/// <remarks>
/// The attributes sit on the record's constructor parameters. ASP.NET Core reads them from
/// there when [ApiController] validates the body, so this helper does the same.
/// </remarks>
public class CreateIncidentRequestValidationTests
{
    private static IncidentsController.CreateIncidentRequest ValidRequest() => new(
        Title: "Bike stolen from rack",
        Description: "Locked bicycle taken from the rack outside the library.",
        IncidentType: "Theft",
        Location: "Downtown",
        Status: null);

    private static List<ValidationResult> Validate(IncidentsController.CreateIncidentRequest request)
    {
        var results = new List<ValidationResult>();
        var type = typeof(IncidentsController.CreateIncidentRequest);
        var ctor = type.GetConstructors().Single();

        foreach (var parameter in ctor.GetParameters())
        {
            var value = type.GetProperty(parameter.Name!)!.GetValue(request);
            var context = new ValidationContext(request) { MemberName = parameter.Name };
            Validator.TryValidateValue(
                value, context, results, parameter.GetCustomAttributes<ValidationAttribute>());
        }

        return results;
    }

    private static bool Mentions(List<ValidationResult> results, string field) =>
        results.Any(r => r.MemberNames.Contains(field));

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        Assert.Empty(Validate(ValidRequest()));
    }

    [Fact]
    public void ValidRequest_WithStatus_HasNoErrors()
    {
        var request = ValidRequest() with { Status = "Open" };

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Title_MissingOrBlank_IsRejected(string? title)
    {
        var request = ValidRequest() with { Title = title! };

        Assert.True(Mentions(Validate(request), nameof(request.Title)));
    }

    [Fact]
    public void Title_TooShort_IsRejected()
    {
        var request = ValidRequest() with { Title = "ab" };

        Assert.True(Mentions(Validate(request), nameof(request.Title)));
    }

    [Fact]
    public void Title_TooLong_IsRejected()
    {
        var request = ValidRequest() with { Title = new string('x', 201) };

        Assert.True(Mentions(Validate(request), nameof(request.Title)));
    }

    [Theory]
    [InlineData(3)]
    [InlineData(200)]
    public void Title_AtLengthLimits_IsAccepted(int length)
    {
        var request = ValidRequest() with { Title = new string('x', length) };

        Assert.Empty(Validate(request));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Description_MissingOrBlank_IsRejected(string? description)
    {
        var request = ValidRequest() with { Description = description! };

        Assert.True(Mentions(Validate(request), nameof(request.Description)));
    }

    [Fact]
    public void Description_TooShort_IsRejected()
    {
        var request = ValidRequest() with { Description = "too short" };   // 9 characters

        Assert.True(Mentions(Validate(request), nameof(request.Description)));
    }

    [Fact]
    public void Description_TooLong_IsRejected()
    {
        var request = ValidRequest() with { Description = new string('x', 2001) };

        Assert.True(Mentions(Validate(request), nameof(request.Description)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IncidentType_MissingOrBlank_IsRejected(string? incidentType)
    {
        var request = ValidRequest() with { IncidentType = incidentType! };

        Assert.True(Mentions(Validate(request), nameof(request.IncidentType)));
    }

    [Fact]
    public void IncidentType_TooLong_IsRejected()
    {
        var request = ValidRequest() with { IncidentType = new string('x', 101) };

        Assert.True(Mentions(Validate(request), nameof(request.IncidentType)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Location_MissingOrBlank_IsRejected(string? location)
    {
        var request = ValidRequest() with { Location = location! };

        Assert.True(Mentions(Validate(request), nameof(request.Location)));
    }

    [Fact]
    public void Location_TooLong_IsRejected()
    {
        var request = ValidRequest() with { Location = new string('x', 201) };

        Assert.True(Mentions(Validate(request), nameof(request.Location)));
    }

    [Fact]
    public void Status_TooLong_IsRejected()
    {
        var request = ValidRequest() with { Status = new string('x', 51) };

        Assert.True(Mentions(Validate(request), nameof(request.Status)));
    }

    [Fact]
    public void MultipleBadFields_ReportsEachOne()
    {
        var request = new IncidentsController.CreateIncidentRequest("", "", "", "", null);

        var results = Validate(request);

        Assert.True(Mentions(results, nameof(request.Title)));
        Assert.True(Mentions(results, nameof(request.Description)));
        Assert.True(Mentions(results, nameof(request.IncidentType)));
        Assert.True(Mentions(results, nameof(request.Location)));
    }
}