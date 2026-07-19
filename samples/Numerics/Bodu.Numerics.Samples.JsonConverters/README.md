# Bodu.Numerics.Samples.JsonConverters

The companion `Bodu.Numerics.Serialization.Json` package that teaches `System.Text.Json` the
`Bodu.Numerics` types — keeping the core library serialization-agnostic (the NodaTime companion-package
pattern). Four scenarios cover the one-call converter registration, the policy-selected wire shapes,
the `Fraction<T>` JSON helpers, and a nested POCO graph.

Everything runs offline with fixed inputs — deterministic output every run.

```bash
dotnet run --project samples/Numerics/Bodu.Numerics.Samples.JsonConverters
```

## Scenario 1 — RegisterConverters

**Intent.** Show the single call that teaches `JsonSerializer` every numerics type:
`AddNumericsJsonConverters()` adds a coherent converter set to a `JsonSerializerOptions`, after which
`Fraction<T>`, `Interval<T>`, `DiscreteInterval<T>`, and `IntervalSet<T>` all round-trip like any
built-in type.

**What it does.** Registers the converters once (default `Strict` policy), then serializes each of the
four types, prints the JSON, deserializes it back, and confirms the re-read value equals the original.

**What to expect.** Each type emits its canonical `Strict` shape — a numerator/denominator object for
the fraction, an endpoint object for the intervals, and a JSON array of piece-objects for the set —
and every round trip reports `matches original: True`:

```text
--- AddNumericsJsonConverters: round-trip every type ---
Fraction<int>         : {"numerator":3,"denominator":4}
  re-read             : 3/4 (matches original: True)
Interval<int>         : {"lower":1,"upper":5,"lowerInclusive":true,"upperInclusive":false}
  re-read             : [1, 5) (matches original: True)
DiscreteInterval<int> : {"lower":10,"upper":20,"lowerInclusive":true,"upperInclusive":true}
  re-read             : [10, 20] (matches original: True)
IntervalSet<int>      : [{"lower":0,"upper":3,"lowerInclusive":true,"upperInclusive":true},{"lower":8,"upper":10,"lowerInclusive":true,"upperInclusive":true}]
  re-read             : [0, 3] ∪ [8, 10] (matches original: True)
```

**APIs demonstrated.** `JsonSerializerOptions.AddNumericsJsonConverters()`,
`JsonSerializer.Serialize`, `JsonSerializer.Deserialize<T>` for `Fraction<int>` / `Interval<int>` /
`DiscreteInterval<int>` / `IntervalSet<int>`.

## Scenario 2 — PolicyShapes

**Intent.** Show how `NumericsJsonPolicy` selects the on-the-wire shape: `Strict` emits
self-describing objects for persistence, while `Compact` emits the terse single-string forms
(`"3/4"`, `"[1, 5)"`). The same value serializes differently under each policy, and each policy reads
back its own shape.

**What it does.** Builds two options instances differing only in the policy passed to
`AddNumericsJsonConverters`, serializes the fraction `3/4` and the interval `[1, 5)` under each, then
deserializes a compact `"3/4"` string to prove the compact reader restores the same value.

**What to expect.** The strict outputs are objects; the compact outputs are single strings — the
fraction as `"3/4"` and the interval in ISO 31-11 bracket notation `"[1, 5)"`:

```text
--- NumericsJsonPolicy: object vs string shapes ---
Fraction strict  : {"numerator":3,"denominator":4}
Fraction compact : "3/4"
Interval strict  : {"lower":1,"upper":5,"lowerInclusive":true,"upperInclusive":false}
Interval compact : "[1, 5)"
compact read-back: "3/4" -> 3/4
```

**APIs demonstrated.** `NumericsJsonPolicy.Strict`, `NumericsJsonPolicy.Compact`,
`AddNumericsJsonConverters(NumericsJsonPolicy)`, `JsonSerializer.Serialize` /
`JsonSerializer.Deserialize<Fraction<int>>`.

## Scenario 3 — FractionExtensions

**Intent.** Show the `FractionJsonExtensions` convenience helpers: `ToJson` and `FromJson` serialize
a single `Fraction<T>` to and from JSON in one call, building the registered options internally so no
`JsonSerializerOptions` plumbing is needed at the call site.

**What it does.** Serializes `22/7` with `ToJson()` (default `Strict`) and `ToJson(Compact)`, then
reads both shapes back with `FromJson<int>` and confirms both restore the original value.

**What to expect.** The default helper emits the object shape and the compact overload the string
shape; both `FromJson` reads restore `22/7`:

```text
--- FractionJsonExtensions: one-call helpers ---
ToJson() strict         : {"numerator":22,"denominator":7}
ToJson(Compact)         : "22/7"
FromJson(strict)        : 22/7
FromJson(Compact)       : 22/7
both restore original   : True
```

**APIs demonstrated.** `FractionJsonExtensions.ToJson<T>(this Fraction<T>)`,
`FractionJsonExtensions.ToJson<T>(this Fraction<T>, NumericsJsonPolicy)`,
`FractionJsonExtensions.FromJson<T>(string)`, `FractionJsonExtensions.FromJson<T>(string,
NumericsJsonPolicy)`.

## Scenario 4 — NestedGraph

**Intent.** Show that once the numerics converters are registered they compose transparently inside a
larger object graph — no per-property attributes required.

**What it does.** Serializes a `Portfolio` POCO whose properties mix a plain string with a
`Fraction<int>`, an `Interval<int>`, and an `IntervalSet<int>`, prints the indented JSON, then
deserializes the whole graph back and reads each property.

**What to expect.** One `Serialize` call renders the whole graph — each numerics property using its
own converter — and one `Deserialize` call reconstructs every property, including the normalized
interval set `[9, 12] ∪ [13, 17]`:

```text
--- NestedGraph: numerics inside a POCO ---
{
  "Name": "Growth",
  "TargetWeight": {
    "numerator": 2,
    "denominator": 5
  },
  "PriceBand": {
    "lower": 90,
    "upper": 110,
    "lowerInclusive": true,
    "upperInclusive": true
  },
  "TradingHours": [
    {
      "lower": 9,
      "upper": 12,
      "lowerInclusive": true,
      "upperInclusive": true
    },
    {
      "lower": 13,
      "upper": 17,
      "lowerInclusive": true,
      "upperInclusive": true
    }
  ]
}
re-read name          : Growth
re-read target weight : 2/5
re-read price band    : [90, 110]
re-read trading hours : [9, 12] ∪ [13, 17]
```

**APIs demonstrated.** `JsonSerializerOptions { WriteIndented = true }.AddNumericsJsonConverters()`,
`JsonSerializer.Serialize` / `JsonSerializer.Deserialize<Portfolio>` over a POCO with `Fraction<int>`,
`Interval<int>`, and `IntervalSet<int>` properties.

## Layout

```text
Bodu.Numerics.Samples.JsonConverters/
  Program.cs                     # runs the scenarios in order
  Portfolio.cs                   # the mixed POCO used by NestedGraph
  Scenarios/RegisterConverters.cs
  Scenarios/PolicyShapes.cs
  Scenarios/FractionExtensions.cs
  Scenarios/NestedGraph.cs
```

## Related

- `Bodu.Numerics.Samples.Fractions` — the exact-rational `Fraction<T>` without the JSON layer.
- `Bodu.Numerics.Samples.Intervals` — the interval algebra without the JSON layer.
```
