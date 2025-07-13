using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Refit.OAuth2.Tests
{
    public class AuthorizationCodeTokenProviderTests
    {
        [Fact]
        public async Task ExchangesAuthorizationCode()
        {
            var handler = new FakeHttpMessageHandler(async req =>
            {
                var body = await req.Content!.ReadAsStringAsync();
                Assert.Contains("grant_type=authorization_code", body);
                Assert.Contains("code=abc", body);
                Assert.Contains("redirect_uri=https%3A%2F%2Fapp", body);
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"tok\",\"expires_in\":3600}")
                };
                return response;
            });

            using var httpClient = new HttpClient(handler);
            var provider = new AuthorizationCodeTokenProvider(
                httpClient,
                "https://auth/token",
                "client",
                "secret",
                "abc",
                "https://app");

            var token = await provider.GetAccessTokenAsync();
            Assert.Equal("tok", token);
        }

        [Fact]
        public async Task CachesTokenUntilExpiration()
        {
            var calls = 0;
            var handler = new FakeHttpMessageHandler(req =>
            {
                calls++;
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"tok\",\"expires_in\":3600}")
                };
                return Task.FromResult(response);
            });

            using var httpClient = new HttpClient(handler);
            var provider = new AuthorizationCodeTokenProvider(
                httpClient,
                "https://auth/token",
                "client",
                "secret",
                "code",
                "https://app");

            var token1 = await provider.GetAccessTokenAsync();
            var token2 = await provider.GetAccessTokenAsync();

            Assert.Equal("tok", token1);
            Assert.Equal("tok", token2);
            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task SendsAdditionalHeaders()
        {
            HttpRequestMessage? captured = null;
            var handler = new FakeHttpMessageHandler(req =>
            {
                captured = req;
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"tok\",\"expires_in\":3600}")
                };
                return Task.FromResult(response);
            });

            using var httpClient = new HttpClient(handler);
            var provider = new AuthorizationCodeTokenProvider(
                httpClient,
                "https://auth/token",
                "client",
                "secret",
                "code",
                "https://app",
                null,
                new Dictionary<string, string> { ["X-Test"] = "1" });

            _ = await provider.GetAccessTokenAsync();
            Assert.True(captured!.Headers.Contains("X-Test"));
        }
    }
}
