using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Refit.OAuth2.Tests
{
    public class OAuth2DelegatingHandlerTests
    {
        private class StubTokenProvider : IOAuth2TokenProvider
        {
            public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
                => Task.FromResult("stub-token");
        }

        [Fact]
        public async Task AddsAuthorizationHeader()
        {
            var handler = new OAuth2DelegatingHandler(new StubTokenProvider())
            {
                InnerHandler = new FakeHttpMessageHandler(req =>
                {
                    Assert.Equal("Bearer", req.Headers.Authorization!.Scheme);
                    Assert.Equal("stub-token", req.Headers.Authorization!.Parameter);
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
                })
            };

            using var client = new HttpClient(handler);
            var response = await client.GetAsync("https://example.com");
            Assert.True(response.IsSuccessStatusCode);
        }
    }
}
