using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Refit.OAuth2
{
    /// <summary>
    /// Retrieves tokens using the OAuth2 client_credentials flow.
    /// </summary>
    public class ClientCredentialsTokenProvider : IOAuth2TokenProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _tokenEndpoint;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string[]? _scopes;
        private readonly IDictionary<string, string>? _additionalHeaders;
        private readonly ILogger<ClientCredentialsTokenProvider>? _logger;
        private string? _accessToken;
        private DateTimeOffset _expiresAt;

        public ClientCredentialsTokenProvider(
            HttpClient httpClient,
            string tokenEndpoint,
            string clientId,
            string clientSecret,
            string[]? scopes = null,
            IDictionary<string, string>? additionalHeaders = null,
            ILogger<ClientCredentialsTokenProvider>? logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokenEndpoint = tokenEndpoint ?? throw new ArgumentNullException(nameof(tokenEndpoint));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
            _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
            _scopes = scopes;
            _additionalHeaders = additionalHeaders;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (_accessToken != null && DateTimeOffset.UtcNow < _expiresAt)
            {
                _logger?.LogDebug("Using cached access token");
                return _accessToken;
            }

            _logger?.LogInformation("Requesting access token from {Endpoint}", _tokenEndpoint);
            using var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(GetParameters())
            };

            if (_additionalHeaders != null)
            {
                foreach (var header in _additionalHeaders)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var payload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(payload);
            _accessToken = doc.RootElement.GetProperty("access_token").GetString();
            var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);
            _logger?.LogInformation("Received access token valid for {Seconds} seconds", expiresIn);
            return _accessToken!;
        }

        private IEnumerable<KeyValuePair<string, string>> GetParameters()
        {
            var parameters = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "client_credentials"),
                new("client_id", _clientId),
                new("client_secret", _clientSecret)
            };
            if (_scopes != null && _scopes.Length > 0)
            {
                parameters.Add(new("scope", string.Join(" ", _scopes)));
            }
            return parameters;
        }
    }
}
