# Bodu.Financial.ExchangeRates.DependencyInjection

> **API stability — Stable.** The public API surface is committed; breaking changes are reserved for a major-version bump per [SemVer](https://semver.org).

Shared dependency-injection machinery for the
[Bodu.Financial](../Bodu.Financial) web exchange-rate providers. Every per-source provider
package (`Boe`, `Ecb`, `Rba`, `Yahoo`, `Ofx`, `Xe`, `Oanda`) delegates its own `Add…`
registration to the single generic extension defined here, so named-`HttpClient`
configuration, resilience, options binding, and provider lifetime are wired one way across
the whole family.

## `AddWebRateProvider`

`AddWebRateProvider<TProvider, TOptions>` registers a `WebRateProvider`
subclass as a singleton, exposed as both `IDatedRateProvider` and
`IRateProvider`. It:

- binds and validates `TOptions` through `Microsoft.Extensions.Options`;
- configures a **named `HttpClient`** for the provider from the options (user agent, HTTP
  timeout) via `IHttpClientFactory`, so the provider never owns the client lifetime;
- layers **Polly** standard resilience over that client, with the attempt / total-request /
  circuit-breaker windows aligned to the configured timeout; and
- constructs the provider from the resolved client, options, and logger.

A provider package's public `Add…` method is a thin call through to this:

```csharp
using Bodu.Financial.ExchangeRates;

// Inside a provider package's own extension:
public static IFinancialServiceBuilder AddAcmeExchangeRates(
    this IFinancialServiceBuilder builder,
    IConfiguration configuration,
    string sectionName = "Financial:Acme",
    Action<AcmeRateProviderOptions>? configure = null) =>
    builder.AddWebRateProvider<AcmeRateProvider, AcmeRateProviderOptions>(
        configuration, sectionName, configure);
```

Both an `IConfiguration`-bound overload and a code-only `Action<TOptions>` overload are
provided, so a provider can be registered from configuration or configured inline.

Consumers do not usually reference this package directly — they add a concrete provider
package (which brings this one transitively) and call its `Add…` method.

Part of the [Bodu](https://github.com/bodu/bodu) utility library.
