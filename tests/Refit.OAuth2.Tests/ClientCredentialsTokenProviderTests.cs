using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Refit.OAuth2.Tests
{
    public class ClientCredentialsTokenProviderTests
    {
        [Fact]
        public async Task RetrievesTokenAndCachesUntilExpiration()
        {
            var calls = 0;
            var handler = new FakeHttpMessageHandler(async request =>
            {
                calls++;
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal("https://auth/token", request.RequestUri!.ToString());
                var body = await request.Content!.ReadAsStringAsync();
                Assert.Contains("grant_type=client_credentials", body);
                Assert.Contains("client_id=myid", body);
                Assert.Contains("client_secret=mysecret", body);
                Assert.Contains("scope=scope1+scope2", body);
                var json = "{\"access_token\":\"abc\",\"expires_in\":3600}";
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                };
                return response;
            });

            using var httpClient = new HttpClient(handler);
            var provider = new ClientCredentialsTokenProvider(
                httpClient,
                "https://auth/token",
                "myid",
                "mysecret",
                new[] { "scope1", "scope2" });

            var token1 = await provider.GetAccessTokenAsync();
            Assert.Equal("abc", token1);
            Assert.Equal(1, calls);

            var token2 = await provider.GetAccessTokenAsync();
            Assert.Equal("abc", token2);
            Assert.Equal(1, calls);
        }

        [Fact]
        public async Task SendsAdditionalHeaders()
        {
            HttpRequestMessage captured = null!;
            var handler = new FakeHttpMessageHandler(req =>
            {
                captured = req;
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"token\",\"expires_in\":3600}")
                };
                return Task.FromResult(response);
            });

            using var httpClient = new HttpClient(handler);
            var provider = new ClientCredentialsTokenProvider(
                httpClient,
                "https://auth/token",
                "id",
                "secret",
                null,
                new Dictionary<string, string> { ["X-Test"] = "1" });

            _ = await provider.GetAccessTokenAsync();
            Assert.True(captured!.Headers.Contains("X-Test"));
        }
    }
}
