# Bodu.Financial.Samples.LiveRates

The one sample in this tree that goes **online**: fetch real published rates from a live web
provider for a computed historical date and its trailing week. The ECB reference-rate feed is
active by default; all ten other providers (RBA, BoE, Yahoo, OFX, OANDA, XE, Fixer,
exchangerate.host, FRED, IMF) are present as comment-switchable blocks — the API-key sources
(Fixer, exchangerate.host, FRED) need a key set and IMF serves daily USD-anchored data — every provider
package is already referenced, so switching is a
comment flip, and the scenarios run unchanged because all providers serve the same
`IDatedRateProvider` contract.

```bash
dotnet run --project samples/Financial/Bodu.Financial.Samples.LiveRates
```

> **Requires internet access.** This is the deliberate exception to the offline-by-default
> sample rule, and it is excluded from the CI samples run for that reason. Without network
> access it prints an explanation and exits with code 1.

## How the date is chosen (and why a rate is near-certain)

The sample never asks for "yesterday" — freshness is where date-based lookups go wrong. Instead:

1. **Buffer:** start from today (UTC) minus 5 days, so publication lag, time zones, and the
   in-progress week can never make the target too fresh.
2. **Weekday:** walk back to the Wednesday on or before that anchor — a Wednesday is never a
   weekend.
3. **Tolerance:** the single-date lookup still carries `PreviousWithin(5)`, so even if that
   Wednesday was a mid-week market holiday, the most recent prior fixing answers — visibly,
   via the result's resolution metadata, never silently.
4. **Window:** the week window is the 7 days ending on that Wednesday — always ≥ 4 business
   days, and comfortably inside every provider's history window (including OANDA's rolling
   ~180 days).

## Scenarios

### HistoricalDate (`Scenarios/HistoricalDate.cs`)

**Intent.** The bread-and-butter historical query — "what was the rate on this date?" — done
safely: an explicit tolerance policy and full resolution/provenance metadata instead of a bare
number.

**What it does.** Resolves the pair for the computed Wednesday with
`GetRate(from, to, date, RateLookupOptions.PreviousWithin(5))` against the warmed provider and
prints the rate, the date actually observed (with the resolution kind and offset), the
publishing source, and the provenance origin.

**What to expect** (values vary by run date — this sample is live, not deterministic):

```
--- Single historical date: EUR/USD on 2026-07-01 ---
Rate            : 1.0842
Observed on     : 2026-07-01 (Exact, offset 0d)
Published by    : ECB
Origin          : Live
```

On a normal week the observed date equals the requested date (`Exact`, offset 0). If the
Wednesday had no fixing you would see `PreviousOnOrBefore` with a small offset instead — the
tolerance doing its job in the open.

**APIs demonstrated.** `EcbRateProvider` construction (owns its `HttpClient` — `using` disposes
it), `WebRateProvider.LoadPairAsync` (the uniform warm-up), `IDatedRateProvider.GetRate` with
`RateLookupOptions.PreviousWithin`, `RateLookupResult` metadata.

### WeekOfRates (`Scenarios/WeekOfRates.cs`)

**Intent.** Range reads are the second core shape: one call for a window, returning exactly
what the source published — business days present, weekends absent — rather than a padded
seven-day array.

**What it does.** Calls `GetRates(from, to, weekStart, lastWednesday)` over the already-warmed
window and prints one line per observation with its weekday, so the business-day shape of the
data is visible.

**What to expect** (values vary by run date):

```
--- A week of rates: EUR/USD 2026-06-25..2026-07-01 ---
5 observations in the window:
  2026-06-25 (Thursday ) 1.0836  [ECB]
  2026-06-26 (Friday   ) 1.0851  [ECB]
  2026-06-29 (Monday   ) 1.0847  [ECB]
  2026-06-30 (Tuesday  ) 1.0839  [ECB]
  2026-07-01 (Wednesday) 1.0842  [ECB]
```

Typically 4–5 observations: the Saturday and Sunday inside the window have no fixings, and
that absence is correct data, not an error. A holiday in the window drops the count by one more.

**APIs demonstrated.** `IDatedRateProvider.GetRates`, `RateRangeResult` (`Count`, `Rates`),
`ExchangeRate` observation fields.

## Switching providers

`Program.cs` contains one commented block per alternative provider with its correct base
currency (RBA quotes AUD, BoE quotes GBP; the pair providers accept any pair). Uncomment one
block, comment the ECB block, and run — the warm-up (`LoadPairAsync`) and both scenarios are
provider-agnostic.

## NuGet equivalent

```bash
dotnet add package Bodu.Financial.ExchangeRates.Ecb    # or .Rba / .Boe / .Yahoo / .Ofx / .Oanda / .Xe / .Fixer / .ExchangeRateHost / .Fred / .Imf
```
