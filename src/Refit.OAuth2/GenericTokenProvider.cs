using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Refit.OAuth2
{
    /// <summary>
    /// Retrieves tokens using arbitrary OAuth2 grant types.
    /// </summary>
    public class GenericTokenProvider : IOAuth2TokenProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _tokenEndpoint;
        private readonly IDictionary<string, string> _parameters;
        private readonly IDictionary<string, string>? _additionalHeaders;
        private readonly ILogger<GenericTokenProvider>? _logger;
        private string? _accessToken;
        private DateTimeOffset _expiresAt;

        public GenericTokenProvider(
            HttpClient httpClient,
            string tokenEndpoint,
            IDictionary<string, string> parameters,
            IDictionary<string, string>? additionalHeaders = null,
            ILogger<GenericTokenProvider>? logger = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _tokenEndpoint = tokenEndpoint ?? throw new ArgumentNullException(nameof(tokenEndpoint));
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
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
                Content = new FormUrlEncodedContent(_parameters)
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
    }
}
