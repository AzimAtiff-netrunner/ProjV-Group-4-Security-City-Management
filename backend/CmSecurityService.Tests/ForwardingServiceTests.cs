using CmSecurityService.Forwarding;
using Microsoft.Extensions.Logging.Abstractions;

namespace CmSecurityService.Tests;

public class ForwardingServiceTests
{
    private class StubEmergencyClient : IEmergencyServiceClient
    {
        public bool ShouldSucceed { get; set; } = true;
        public int CallCount { get; private set; }

        public Task<EmergencyServiceResponse?> ForwardIncidentAsync(
            EmergencyIncidentPayload payload,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (!ShouldSucceed)
            {
                return Task.FromResult<EmergencyServiceResponse?>(null);
            }

            return Task.FromResult<EmergencyServiceResponse?>(
                new EmergencyServiceResponse($"EMG-{CallCount:D4}", "Accepted", DateTime.UtcNow));
        }
    }

    [Theory]
    [InlineData("Structure Fire", "Heavy smoke on 2nd floor", true, "Fire")]
    [InlineData("Medical", "Unconscious person near lobby", true, "EMS")]
    [InlineData("Noise Complaint", "Loud music after midnight", false, "Police")]
    public void ClassifyCall_IdentifiesFireAndEmsAccurately(
        string type, string desc, bool expectedForward, string expectedTarget)
    {
        var service = new InMemoryForwardingService(new StubEmergencyClient(), NullLogger<InMemoryForwardingService>.Instance);

        var result = service.RequiresEmergencyForwarding(type, desc, out var recommended);

        Assert.Equal(expectedForward, result);
        Assert.Equal(expectedTarget, recommended);
    }

    [Fact]
    public async Task ForwardCallAsync_WhenServiceAvailable_RecordsSuccessAndExternalId()
    {
        var stubClient = new StubEmergencyClient { ShouldSucceed = true };
        var service = new InMemoryForwardingService(stubClient, NullLogger<InMemoryForwardingService>.Instance);

        var result = await service.ForwardCallAsync(new ForwardCallRequest(101, "Fire", "Kitchen fire"));

        Assert.Equal("Forwarded", result.Status);
        Assert.Equal("EMG-0001", result.ExternalReferenceId);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(1, result.AttemptCount);
    }

    [Fact]
    public async Task ForwardCallAsync_WhenServiceFails_MarksFailedAndAllowsSafeRetryWithoutDuplicates()
    {
        var stubClient = new StubEmergencyClient { ShouldSucceed = false };
        var service = new InMemoryForwardingService(stubClient, NullLogger<InMemoryForwardingService>.Instance);
        var request = new ForwardCallRequest(202, "EMS", "Sprained ankle");

        var firstAttempt = await service.ForwardCallAsync(request);
        Assert.Equal("Failed", firstAttempt.Status);
        Assert.Null(firstAttempt.ExternalReferenceId);
        Assert.NotNull(firstAttempt.ErrorMessage);
        Assert.Equal(1, firstAttempt.AttemptCount);

        stubClient.ShouldSucceed = true;
        var retryAttempt = await service.ForwardCallAsync(request);
        Assert.Equal("Forwarded", retryAttempt.Status);
        Assert.Equal("EMG-0002", retryAttempt.ExternalReferenceId);
        Assert.Equal(2, retryAttempt.AttemptCount);

        var duplicateAttempt = await service.ForwardCallAsync(request);
        Assert.Equal("EMG-0002", duplicateAttempt.ExternalReferenceId);
        Assert.Equal(2, stubClient.CallCount);
    }
}
