# Bodu.Numerics.Serialization.Json

> **API stability — Preview.** This package tracks the Preview-tier `Bodu.Numerics` surface; the converter set and wire shapes are settled but may receive source-breaking refinement before the 1.0 stable release. Breaking changes are otherwise reserved for a major-version bump per [SemVer](https://semver.org).

`System.Text.Json` integration for **[Bodu.Numerics](https://www.nuget.org/packages/Bodu.Numerics)**. The core library is deliberately serialization-agnostic — its value types carry no `[JsonConverter]` attribute and take no `System.Text.Json` dependency — so JSON support is opt-in through this companion package (the NodaTime companion-package pattern):

- **`ConfigureForBoduNumerics(options, policy)`** — one call registers a coherent converter set for every serializable `Bodu.Numerics` type.
- Converters (+ factories for the generic types) for **`Fraction<T>`**, **`Interval<T>`**, **`DiscreteInterval<T>`**, **`IntervalSet<T>`**, and **`BigDecimal`**.
- **`NumericsJsonPolicy`** — `Strict` (canonical object shapes, the default), `Lenient` (Strict plus read tolerance for external feeds), `Compact` (single strings such as `"3/4"` and `"[1, 5)"`).
- The transient result types `IntervalPair<T>` / `DiscreteIntervalPair<T>` are deliberately **not** serializable — call `ToIntervalSet()` and persist the set instead.

## Installation

```shell
dotnet add package Bodu.Numerics.Serialization.Json
```

Targets `net8.0`. Depends on `Bodu.Numerics`.

## Usage

Register the converters once, before the options instance is first used:

```csharp
using System.Text.Json;
using Bodu.Numerics;
using Bodu.Numerics.Serialization.Json;

var options = new JsonSerializerOptions().ConfigureForBoduNumerics();   // Strict

string json = JsonSerializer.Serialize(new Fraction<int>(3, 4), options);
// → {"numerator":3,"denominator":4}

var compact = new JsonSerializerOptions()
    .ConfigureForBoduNumerics(NumericsJsonPolicy.Compact);
// Fraction → "3/4", Interval → "[1, 5)", empty interval → "∅"
```

One policy value shapes every registered converter:

| Policy | Fraction | Interval family | Intended use |
|---|---|---|---|
| `Strict` (default) | object `{ "numerator": 3, "denominator": 4 }` | object with `lower` / `upper` / inclusivity flags, `{ "empty": true }`, or unbounded markers | Canonical persistence and interchange. |
| `Lenient` | as `Strict`, plus a top-level string accepted on read | as `Strict`, plus `"min"` / `"max"` aliases and defaulted inclusivity | Spreadsheet / external-feed ingest. Writes as `Strict`. |
| `Compact` | string `"3/4"` | ISO 31-11 bracket string `"[1, 5)"`, `"∅"` for empty | Compact payloads where size matters. |

`BigDecimal` serializes as the canonical object `{ "unscaledValue": 12340, "scale": 3 }` under `Strict` — the unscaled value is a raw JSON number, so an arbitrary-magnitude mantissa round-trips exactly — and as the plain decimal string `"12.340"` under `Compact` (a string rather than a bare number, because many consumers narrow long JSON numbers to `double`). `Lenient` reads either shape.

Converters can also be registered individually (for example only fractions, or a single closed backing type) via `options.Converters.Add(new FractionJsonConverterFactory(NumericsJsonPolicy.Compact))`.

## Documentation

See the [JSON serialization guide](https://github.com/bslater/bodu/blob/master/docs/guides/numerics/json-serialization.md) for the full wire-shape reference, error behaviour, and per-converter registration.

## License

MIT. © Bodu Pty. Ltd.
