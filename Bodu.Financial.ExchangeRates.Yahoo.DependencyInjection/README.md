# Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection

Dependency-injection extensions for
[`Bodu.Financial.ExchangeRates.Yahoo`](../Bodu.Financial.ExchangeRates.Yahoo).

Registers `YahooExchangeRateProvider` as a singleton backed by an
`IHttpClientFactory`-managed `HttpClient`, binds `YahooExchangeRateOptions` through
`Microsoft.Extensions.Options`, and exposes the provider through the
`IDatedExchangeRateProvider` and `IExchangeRateProvider` contracts.

```csharp
using Bodu.Financial.ExchangeRates.Yahoo.DependencyInjection;

// One-call entry point: core Bodu.Financial services + the Yahoo provider.
services.AddYahooExchangeRates(configuration);

// Or compose onto an existing financial builder.
services
    .AddFinancialService(configuration)
    .AddYahooExchangeRates(configuration, configure: o => o.DefaultLookback = TimeSpan.FromDays(14));
```

Options bind from the `Financial:Yahoo` configuration section by default, for example:

```json
{
  "Financial": {
    "Yahoo": {
      "BaseAddress": "https://query2.finance.yahoo.com/",
      "ChartPath": "v8/finance/chart/{symbol}",
      "SymbolFormat": "{from}{to}=X"
    }
  }
}
```
