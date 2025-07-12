using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Refit.OAuth2
{
    /// <summary>
    /// Extension methods for adding OAuth2 handlers to Refit clients.
    /// </summary>
    public static class OAuth2HttpClientBuilderExtensions
    {
        /// <summary>
        /// Adds an OAuth2 client credentials handler to the HTTP client.
        /// </summary>
        public static IHttpClientBuilder AddOAuth2ClientCredentials(
            this IHttpClientBuilder builder,
            HttpClient tokenClient,
            string tokenEndpoint,
            string clientId,
            string clientSecret,
            string[]? scopes = null,
            IDictionary<string, string>? additionalHeaders = null,
            ILoggerFactory? loggerFactory = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.AddHttpMessageHandler(sp =>
            {
                var providerLogger = loggerFactory?.CreateLogger<ClientCredentialsTokenProvider>()
                    ?? sp.GetService<ILogger<ClientCredentialsTokenProvider>>();
                var handlerLogger = loggerFactory?.CreateLogger<OAuth2DelegatingHandler>()
                    ?? sp.GetService<ILogger<OAuth2DelegatingHandler>>();
                var provider = new ClientCredentialsTokenProvider(
                    tokenClient,
                    tokenEndpoint,
                    clientId,
                    clientSecret,
                    scopes,
                    additionalHeaders,
                    providerLogger);
                return new OAuth2DelegatingHandler(provider, handlerLogger);
            });

            return builder;
        }

        /// <summary>
        /// Adds an OAuth2 authorization code handler to the HTTP client.
        /// </summary>
        public static IHttpClientBuilder AddOAuth2AuthorizationCode(
            this IHttpClientBuilder builder,
            HttpClient tokenClient,
            string tokenEndpoint,
            string clientId,
            string clientSecret,
            string code,
            string redirectUri,
            string[]? scopes = null,
            IDictionary<string, string>? additionalHeaders = null,
            ILoggerFactory? loggerFactory = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            builder.AddHttpMessageHandler(sp =>
            {
                var providerLogger = loggerFactory?.CreateLogger<AuthorizationCodeTokenProvider>()
                    ?? sp.GetService<ILogger<AuthorizationCodeTokenProvider>>();
                var handlerLogger = loggerFactory?.CreateLogger<OAuth2DelegatingHandler>()
                    ?? sp.GetService<ILogger<OAuth2DelegatingHandler>>();
                var provider = new AuthorizationCodeTokenProvider(
                    tokenClient,
                    tokenEndpoint,
                    clientId,
                    clientSecret,
                    code,
                    redirectUri,
                    scopes,
                    additionalHeaders,
                    providerLogger);
                return new OAuth2DelegatingHandler(provider, handlerLogger);
            });

            return builder;
        }
    }
}
