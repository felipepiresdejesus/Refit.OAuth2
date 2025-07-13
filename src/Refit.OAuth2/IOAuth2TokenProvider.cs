using System.Threading;
using System.Threading.Tasks;

namespace Refit.OAuth2
{
    /// <summary>
    /// Abstraction for retrieving OAuth2 access tokens.
    /// </summary>
    public interface IOAuth2TokenProvider
    {
        /// <summary>
        /// Gets a valid access token.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The access token string.</returns>
        Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    }
}
