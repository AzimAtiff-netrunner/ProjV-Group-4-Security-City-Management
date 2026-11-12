using System.Net.Http.Json;

namespace CmSecurityService.Forwarding;

public class EmergencyServiceClient : IEmergencyServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EmergencyServiceClient> _logger;

    public EmergencyServiceClient(HttpClient httpClient, ILogger<EmergencyServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EmergencyServiceResponse?> ForwardIncidentAsync(
        EmergencyIncidentPayload payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/emergency/v1/incidents", payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError(
                    "Emergency Service call failed with HTTP {StatusCode}: {ErrorBody}",
                    (int)response.StatusCode,
                    errorBody);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<EmergencyServiceResponse>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network connectivity error while contacting Emergency Service.");
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while waiting for Emergency Service response.");
            return null;
        }
    }
}
