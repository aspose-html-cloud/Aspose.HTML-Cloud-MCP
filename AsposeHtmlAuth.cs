using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aspose.HTML.Cloud.Mcp;

/// <summary>
/// Handles authentication against the Aspose identity server.
/// Credentials are resolved from environment variables ASPOSE_CLIENT_ID and ASPOSE_CLIENT_SECRET.
/// </summary>
internal sealed class AsposeHtmlAuth
{
    private static readonly string TokenEndpoint = "https://api.aspose.cloud/connect/token";

    // Cache the token so we don't re-fetch on every tool call within one server session.
    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly HttpClient _http;

    public AsposeHtmlAuth(HttpClient http)
    {
        _http = http;
        _clientId = Environment.GetEnvironmentVariable("ASPOSE_CLIENT_ID")
            ?? throw new InvalidOperationException(
                "Environment variable ASPOSE_CLIENT_ID is not set. " +
                "Get your free credentials at https://dashboard.aspose.cloud/");
        _clientSecret = Environment.GetEnvironmentVariable("ASPOSE_CLIENT_SECRET")
            ?? throw new InvalidOperationException(
                "Environment variable ASPOSE_CLIENT_SECRET is not set. " +
                "Get your free credentials at https://dashboard.aspose.cloud/");
    }

    /// <summary>Returns a valid bearer token, fetching a new one when the cached one is expired.</summary>
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        var body = new StringContent(
            $"grant_type=client_credentials&client_id={Uri.EscapeDataString(_clientId)}&client_secret={Uri.EscapeDataString(_clientSecret)}",
            Encoding.UTF8,
            "application/x-www-form-urlencoded");

        var response = await _http.PostAsync(TokenEndpoint, body, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to obtain Aspose bearer token: {response.StatusCode} - {error}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json)
            ?? throw new InvalidOperationException("Unexpected empty token response.");

        _cachedToken = tokenResponse.AccessToken;
        // Subtract a 30-second buffer to avoid using a just-expired token
        _tokenExpiry = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 30);

        return _cachedToken;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }
    }
}
