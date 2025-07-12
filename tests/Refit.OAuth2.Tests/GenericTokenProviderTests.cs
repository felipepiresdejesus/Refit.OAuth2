using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Refit.OAuth2.Tests
{
    public class GenericTokenProviderTests
    {
        [Fact]
        public async Task SendsCustomParameters()
        {
            var handler = new FakeHttpMessageHandler(async req =>
            {
                var body = await req.Content!.ReadAsStringAsync();
                Assert.Contains("grant_type=password", body);
                Assert.Contains("username=user", body);
                Assert.Contains("password=pass", body);
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"tok\",\"expires_in\":3600}")
                };
                return response;
            });

            using var httpClient = new HttpClient(handler);
            var parameters = new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["username"] = "user",
                ["password"] = "pass",
                ["client_id"] = "cid",
                ["client_secret"] = "sec"
            };
            var provider = new GenericTokenProvider(httpClient, "https://auth/token", parameters);
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
            var parameters = new Dictionary<string, string> { ["grant_type"] = "client_credentials" };
            var provider = new GenericTokenProvider(httpClient, "https://auth/token", parameters);

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
            var parameters = new Dictionary<string, string> { ["grant_type"] = "client_credentials" };
            var provider = new GenericTokenProvider(
                httpClient,
                "https://auth/token",
                parameters,
                new Dictionary<string, string> { ["X-Test"] = "1" });

            _ = await provider.GetAccessTokenAsync();
            Assert.True(captured!.Headers.Contains("X-Test"));
        }
    }
}
