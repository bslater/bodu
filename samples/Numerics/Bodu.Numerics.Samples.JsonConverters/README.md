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
--- AddNumericsJsonConverters - round-trip every type ---
  What   : Registers the converters once on a JsonSerializerOptions, then serializes and re-reads each numerics
           type, comparing the result against the original value.
  Why    : Bodu.Numerics deliberately takes no dependency on System.Text.Json, so the converters ship in a companion
           package - the pattern NodaTime uses. That keeps the core usable where the serializer is not, at the cost
           of one registration call. Without it these types serialize by their public properties, which for a
           Fraction means emitting whatever surface it happens to expose rather than a form that reads back.
  Expect : Each type emits a documented shape and reads back equal to what went in - the match is the claim, not the
           text. IntervalSet emits an array because it is a union of pieces, so its normalized form survives the
           round trip rather than being flattened.

  Fraction<int>         : {"numerator":3,"denominator":4}
    re-read             : 3/4  (matches original: True - the equality is the claim; the text above is just how it got there)
  Interval<int>         : {"lower":1,"upper":5,"lowerInclusive":true,"upperInclusive":false}
    re-read             : [1, 5)  (matches original: True - the equality is the claim; the text above is just how it got there)
  DiscreteInterval<int> : {"lower":10,"upper":20,"lowerInclusive":true,"upperInclusive":true}
    re-read             : [10, 20]  (matches original: True - the equality is the claim; the text above is just how it got there)
  IntervalSet<int>      : [{"lower":0,"upper":3,"lowerInclusive":true,"upperInclusive":true},{"lower":8,"upper":10,"lowerInclusive":true,"upperInclusive":true}]
    re-read             : [0, 3] ∪ [8, 10]  (matches original: True - the equality is the claim; the text above is just how it got there)
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
--- NumericsJsonPolicy - object vs string shapes ---
  What   : Writes the same Fraction and Interval under the strict object policy and the compact string policy, then
           reads a compact value back.
  Why    : The two shapes trade legibility against tooling. The object form is self-describing, so a consumer that
           has never seen a Fraction can still read it, and a JSON Schema can validate it. The compact form is a
           single string - far easier for a human to read in a config file or a log line, and much smaller in bulk -
           but its meaning lives in a parser rather than in the document. Pick the object form for an API contract
           and the compact one for storage you control.
  Expect : The same values in both shapes: an object with named fields, and "3/4" or "[1, 5)" as a string. The
           compact form reads back to an equal value, so the choice is about the document rather than about
           fidelity.

  Fraction strict  : {"numerator":3,"denominator":4}  (self-describing: a consumer that has never seen a Fraction can still read it, and a schema can validate it)
  Fraction compact : "3/4"  (one string - far smaller in bulk and readable in a config file, but its meaning lives in a parser rather than the document)
  Interval strict  : {"lower":1,"upper":5,"lowerInclusive":true,"upperInclusive":false}  (the inclusivity flags are explicit fields, so no reader has to know the bracket convention)
  Interval compact : "[1, 5)"  (the same information carried by the brackets - compact, but only if the reader knows what [ and ) mean)
  compact read-back: "3/4" -> 3/4  (the compact form loses nothing: the choice is about the document, not about fidelity)
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
--- FractionJsonExtensions - one-call helpers ---
  What   : Uses ToJson and FromJson on a Fraction under both policies, and confirms both routes restore the original
           value.
  Why    : The extensions exist for the case where one value crosses a boundary and building a JsonSerializerOptions
           would be ceremony - a log line, a cache key, a single column. They are a convenience over the same
           converters, not a second implementation, which is what keeps the two routes from drifting. For a whole
           object graph the registration is still the right approach, since options should be built once and reused
           rather than per call.
  Expect : Both policies round-trip 22/7, and both restore it equal to the original. Same converters underneath, so
           the output matches the registered route exactly.

  ToJson() strict         : {"numerator":22,"denominator":7}  (identical to the registered route - one converter implementation, two ways to reach it)
  ToJson(Compact)         : "22/7"  (the policy is an argument here rather than options state, which is what makes this a one-liner)
  FromJson(strict)        : 22/7  (for a single value crossing a boundary - a log line, a cache key, one column)
  FromJson(Compact)       : 22/7  (the same value from the other shape)
  both restore original   : True  (expected True - for a whole graph still prefer the registration, since options should be built once and reused)
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
--- NestedGraph - numerics inside a POCO ---
  What   : Serializes an ordinary class holding a Fraction, an Interval and a list of intervals, then reads the
           whole graph back and compares each member.
  Why    : Registering converters on the options is what makes these types work anywhere in a graph, at any depth,
           including inside collections - rather than only when serialized directly. That is the practical
           difference between a converter package and a helper method: the POCO needs no attributes, no custom
           converter of its own, and no awareness that its members are anything unusual.
  Expect : The nested members emit exactly the shapes the standalone scenario produced, and the list of intervals
           serializes element by element. Every member compares equal after the round trip, which is what proves
           depth is irrelevant to the registration.

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
  re-read name          : Growth  (an ordinary property, serialized the ordinary way)
  re-read target weight : 2/5  (a Fraction nested one level down - the POCO needs no attribute and no converter of its own)
  re-read price band    : [90, 110]  (an Interval, with its inclusivity preserved through the round trip)
  re-read trading hours : [9, 12] ∪ [13, 17]  (inside a collection, which is the case a helper method cannot reach and a registered converter handles for free)
```

**APIs demonstrated.** `JsonSerializerOptions { WriteIndented = true }.AddNumericsJsonConverters()`,
`JsonSerializer.Serialize` / `JsonSerializer.Deserialize<Portfolio>` over a POCO with `Fraction<int>`,
`Interval<int>`, and `IntervalSet<int>` properties.

## Layout

```text
Bodu.Numerics.Samples.JsonConverters/
  Program.cs                     # runs the scenarios in order
  SampleConsole.cs               # the what/why/expect banner every scenario opens with
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
