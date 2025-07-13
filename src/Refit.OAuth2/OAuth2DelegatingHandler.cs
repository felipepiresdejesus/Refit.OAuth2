using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Refit.OAuth2
{
    /// <summary>
    /// DelegatingHandler that attaches an OAuth2 access token to outgoing requests.
    /// </summary>
    public class OAuth2DelegatingHandler : DelegatingHandler
    {
        private readonly IOAuth2TokenProvider _tokenProvider;
        private readonly ILogger<OAuth2DelegatingHandler>? _logger;

        public OAuth2DelegatingHandler(IOAuth2TokenProvider tokenProvider, ILogger<OAuth2DelegatingHandler>? logger = null)
        {
            _tokenProvider = tokenProvider;
            _logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            _logger?.LogDebug("Attaching bearer token to request");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
