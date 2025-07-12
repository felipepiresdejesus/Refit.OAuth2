# Refit.OAuth2

Simple helpers for integrating Refit clients with OAuth2 authentication without relying on IdentityModel or Duende packages.

## Features

- Supports all standard OAuth2 flows including client credentials,
  authorization code, resource owner password and refresh token. Custom
  grant types can also be used via a generic provider.
- `DelegatingHandler` that automatically attaches Bearer tokens.
- Extension methods for registering Refit clients with OAuth2.

## Usage

```csharp
var services = new ServiceCollection();
var tokenHttpClient = new HttpClient();
services
    .AddRefitClient<IMyApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddOAuth2ClientCredentials(
        tokenHttpClient,
        tokenEndpoint: "https://auth.example.com/connect/token",
        clientId: "client-id",
        clientSecret: "client-secret",
        scopes: new[] { "api.read" },
        additionalHeaders: new Dictionary<string, string>
        {
            ["X-Custom-Header"] = "value"
        });

// Optionally customize the loggers
services
    .AddRefitClient<IOtherApi>()
    .AddOAuth2ClientCredentials(
        tokenHttpClient,
        "https://auth.example.com/connect/token",
        "client-id",
        "client-secret",
        loggerFactory: LoggerFactory.Create(b => b.AddConsole()));

var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IMyApi>();
```

## Building a NuGet package

To create a package for distribution run:

```bash
dotnet pack src/Refit.OAuth2/Refit.OAuth2.csproj -c Release
```

The resulting `.nupkg` file will be placed in the `bin/Release` directory and
can be published to a NuGet feed.

This library aims to be compliant with the OAuth2 specification and can be
extended with custom grant types if needed.
