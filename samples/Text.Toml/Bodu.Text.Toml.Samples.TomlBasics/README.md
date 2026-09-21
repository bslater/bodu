# Bodu.Text.Toml.Samples.TomlBasics

The starting point for `Bodu.Text.Toml`: the `TomlSerializer` POCO surface, shaped after
`System.Text.Json`. Four scenarios cover the serialize/deserialize loop, TOML's four native
date-time kinds, how wire names are chosen (naming policies vs the attribute family), and the
two wire-level knobs (`SpecVersion`, `ByteArrayHandling`).

Everything runs offline against the committed `Data/app-config.toml` — no network, no
machine-specific state, deterministic output.

```bash
dotnet run --project samples/Text.Toml/Bodu.Text.Toml.Samples.TomlBasics
```

## Scenario 1 — SerializerRoundTrip

**Intent.** Show the core workflow every consumer starts with: a TOML file on disk becomes a
typed object graph in one call, and the graph becomes TOML again in one call — the
`System.Text.Json` workflow (`Deserialize<T>` / `Serialize<T>`), for TOML.

**What it does.** Reads `Data/app-config.toml` (a scalar section, a nested `[database]`
table, and a `[[endpoints]]` array of tables) and deserializes it into the `AppConfig` POCO
using the `SnakeCaseLower` naming policy, so `service_name` binds to `ServiceName` without
any attributes. It then mutates `MaxRetries`, serializes the graph back to TOML text, and
deserializes that text again to prove the round trip loses nothing.

**What to expect.** The banner lines echo the typed values that arrived from the file —
service name `orders`, database `localhost:5432`, and both endpoints with their URLs —
followed by a round-trip line confirming the mutated retry count (`5`) and both endpoints
survived the re-parse:

```text
--- TomlSerializer - file to POCO to TOML and back ---
  What   : Deserializes the committed config file into a typed object graph with a nested table and an array of
           tables, reads the values, changes one, serializes the graph back to TOML, and deserializes that result
           again.
  Why    : TOML exists because INI has no specification and JSON is awkward to hand-edit - it is a configuration
           format with a grammar, which means a document can be validated rather than merely parsed. The serializer
           is deliberately shaped like System.Text.Json so the options, attributes and naming policies are the ones
           a .NET developer already knows. The round trip is the interesting assertion here rather than the
           deserialize: a serializer that reads a format but cannot re-emit it is a parser, and config tooling
           usually needs to write the file back.
  Expect : Both structural shapes survive: the nested table binds to a class property and the array of tables to a
           list. After the edit the re-emitted document parses back with the changed value and the same number of
           endpoints, so re-emitting did not flatten or reorder the structure.

  Service   : orders (enabled: True, retries: 3)
  Database  : localhost:5432  (a [database] table binds to a nested class - TOML structure maps onto object structure)
  Endpoints : public -> https://api.example.com, admin -> https://admin.example.com  (repeated [[endpoints]] blocks bind to a list - the array-of-tables form TOML uses for collections)
  Round trip: retries 5, endpoints 2 (values preserved)  (the edit survives and the structure does not flatten - a serializer that cannot re-emit is only a parser)
```

**APIs demonstrated.** `TomlSerializer.Deserialize<T>(string, TomlSerializerOptions)`,
`TomlSerializer.Serialize<T>`, `TomlSerializerOptions.PropertyNamingPolicy`,
`TomlNamingPolicy.SnakeCaseLower`, nested tables and arrays of tables binding to POCOs and
`List<T>`.

## Scenario 2 — TemporalKinds

**Intent.** Demonstrate TOML's headline feature over JSON: date-time values are native, and
there are *four distinct kinds* — offset date-time (an exact instant), local date-time, local
date, and local time (deliberately zone-less). The serializer maps each kind to the matching
.NET type instead of forcing everything through `DateTime`.

**What it does.** Deserializes the same `Data/app-config.toml`, whose `released_on` is a TOML
local date, `maintenance_window` a local time, and `build_stamp` an offset date-time. It
prints each value as its natural .NET type (`DateOnly`, `TimeOnly`, `DateTimeOffset` — no
invented midnights, no UTC guesses), then serializes the graph back and greps the emitted
lines to show each .NET type re-emitting as its distinct TOML kind.

**What to expect.** Three lines showing the typed values (including the preserved `+10:00`
offset on the build stamp), then the three wire lines — note the local date has no time
component, the local time no date, and only the offset date-time carries a zone:

```text
--- The four native temporal kinds ---
  What   : Deserializes a document carrying a local date, a local time and an offset date-time, shows which .NET
           type each became, then serializes the graph back and prints the wire form of each.
  Why    : This is TOML's clearest advantage over JSON for configuration. JSON has no date type, so every timestamp
           is a string and every consumer re-invents the parsing - which is how a release date acquires an invented
           midnight and a local maintenance window silently becomes UTC. TOML distinguishes four kinds, and the
           distinction is semantic rather than cosmetic: an offset date-time names an exact instant, while a local
           date-time deliberately does not, because '02:00 on Sunday' for a maintenance window means 02:00 wherever
           the server is. Mapping them onto DateOnly, TimeOnly and DateTimeOffset preserves that distinction in the
           type system instead of losing it at the boundary.
  Expect : Each value arrives as the .NET type that carries exactly the information the document stated - no
           invented midnight on the date, no assumed zone on the time. Serializing back produces the same three
           distinct wire forms, so the round trip does not quietly promote a local value to an instant.

  released_on        -> DateOnly       : 2026-01-15  (a date with no time component - DateTime would have invented a midnight that the document never stated)
  maintenance_window -> TimeOnly       : 03:30  (a wall-clock time carrying no zone, deliberately - a maintenance window means that hour wherever the server is)
  build_stamp        -> DateTimeOffset : 2026-01-15T08:30:00+10:00  (the one kind that names an exact instant, so its offset is part of the value rather than an assumption)
  wire: released_on = 2026-01-15
  wire: maintenance_window = 03:30:00
  wire: build_stamp = 2026-01-15T08:30:00+10:00
  (three distinct wire forms back out - the round trip does not promote a local value to an instant)
```

**APIs demonstrated.** TOML local date → `DateOnly`, local time → `TimeOnly`, offset
date-time → `DateTimeOffset`; the same mappings in reverse on `Serialize`.

## Scenario 3 — NamingAndAttributes

**Intent.** Explain the precedence rules for wire names: a naming policy maps every property
by convention; the attribute family overrides it per member. Consumers should reach for the
policy first and attributes only for the exceptions — this scenario shows both layers and
where each wins.

**What it does.** Serializes one small POCO under three policies (`CamelCase`,
`SnakeCaseLower`, `KebabCaseLower`) to show that only key spelling changes. It then
round-trips the shared `AppConfig` and verifies the three attribute behaviours: `Address`
writes as `url` (`[TomlPropertyName]` beats the policy), the computed `DisplayLabel` never
reaches the wire (`[TomlIgnore]`), and deserializing a document missing `service_name` throws
`TomlSerializationException` up front (`[TomlRequired]`) instead of silently yielding an
empty string. Finally it contrasts enum output: the default emits the .NET member name
verbatim, while `TomlStringEnumConverter` re-cases it through its own naming policy.

**What to expect.** The same first property spelled three ways, two `True` confirmations for
the attribute overrides, the exact `[TomlRequired]` rejection message naming the missing key,
and the enum key emitted as `"Wednesday"` by default vs `"wednesday"` once the converter's
snake-case policy applies:

```text
--- Naming policies, attributes, and enum converters ---
  What   : Serializes one POCO under three naming policies, shows a property renamed by attribute and another
           excluded entirely, provokes the missing-required-key failure, then contrasts default enum output with a
           policy-cased converter.
  Why    : The wire name and the C# name answer to different audiences. TOML files are hand-edited, so their keys
           follow the format's conventions rather than .NET's, and a policy handles that for the whole type at once
           instead of an attribute per property. Attributes then exist for the cases a convention cannot reach: a
           key whose spelling is fixed by an external contract, a computed property that should never reach the
           wire, and a key whose absence is an error rather than a default. That last one matters most - without it
           a missing service_name deserializes to an empty string and the failure surfaces somewhere unrelated, long
           after the config was read.
  Expect : One type produces three different key spellings with no change to the class. The renamed property appears
           under its wire name and the ignored one is absent from the output entirely. The missing required key is
           rejected at deserialize time naming the key, rather than producing an object with a silently empty field.
           The enum rows show the same value spelled two ways depending on the converter.

  CamelCase     : jobName = "nightly-sync"
  SnakeCaseLower: job_name = "nightly-sync"
  KebabCaseLower: job-name = "nightly-sync"
  (one class, three key spellings - the convention belongs to the file format, not to the C# type)
  [PropertyName] : endpoints write 'url =' -> True  (expected True - the attribute overrides the policy where an external contract fixes the spelling)
  [Ignore]       : 'display_label' absent  -> True  (expected True - a computed property has no business on the wire, where it would look editable)
  [Required]     : missing key rejected   -> Required member 'service_name' was not present in the input for type 'Bodu.Text.Toml.Samples.TomlBasics.AppConfig'.  (without this the field would deserialize to an empty string and fail somewhere unrelated, much later)
  enum default       : run_day = "Wednesday"  (the .NET member name verbatim)
  enum policy-cased  : run_day = "wednesday"  (re-cased through its own policy, and configured to reject integer input so a stray number is an error rather than a silent enum value)
```

**APIs demonstrated.** `TomlNamingPolicy.CamelCase` / `.SnakeCaseLower` / `.KebabCaseLower`,
`[TomlPropertyName]`, `[TomlIgnore]`, `[TomlRequired]`, `TomlSerializationException`,
`TomlSerializerOptions.Converters`, `TomlStringEnumConverter(namingPolicy,
allowIntegerValues)`.

## Scenario 4 — SpecVersionAndBytes

**Intent.** Surface the two wire-level knobs a consumer eventually needs: `SpecVersion` gates
the TOML v1.1.0 grammar extensions on parse (v1.0.0 stays the strict default — important when
your files must interoperate with v1.0-only tooling), and `ByteArrayHandling` chooses how
`byte[]` travels, since TOML has no native binary type.

**What it does.** Attempts to parse a document using a `\x` hex escape — a v1.1.0-only string
feature — under the default `V1_0` (rejected with `TomlFormatException`) and again under
`V1_1` (accepted, decoding to `café`). It then serializes a `byte[]` payload both ways:
`IntegerArray` (the self-describing default) and `Base64String` (compact), and round-trips
the Base64 form back to bytes to show the handling must match on both sides.

**What to expect.** The rejection message under v1.0, the successfully decoded string under
v1.1, the two wire shapes for the same four bytes, and the restored payload:

```text
--- Spec version gating and byte-array wire shapes ---
  What   : Parses a document using a TOML v1.1 escape under the default spec version and again with v1.1 enabled,
           then serializes the same byte array under both wire shapes and round-trips one of them.
  Why    : Pinning the spec version is a compatibility decision that runs in both directions. Accepting v1.1 grammar
           by default would mean this library happily reads documents that other TOML parsers reject, so a config
           file that works here fails in the next tool that touches it - which is why v1.0.0 is the default and the
           newer grammar is opt-in. Byte arrays are a different kind of choice: TOML has no binary type, so the
           bytes have to be encoded as something, and neither answer is universally right. An integer array is
           readable and editable by hand; Base64 is compact and does not turn a kilobyte into a wall of numbers.
  Expect : The same document is rejected under the default and accepted under v1.1, which is the gate working - the
           parser is strict by default rather than permissive. The two byte shapes are visibly different encodings
           of identical bytes, and the round trip returns the original payload provided the reader is told which
           shape to expect, since the wire form alone does not say.

  V1_0 (default): rejected -> The escape sequence is not valid.  (strict by default, so a document accepted here is one other TOML parsers will also accept)
  V1_1          : accepted -> greeting = "café"  (the newer grammar is opt-in - raising the version is a deliberate compatibility decision)
  IntegerArray  : payload = [222, 173, 190, 239]  (readable and hand-editable, but a kilobyte of payload becomes a wall of numbers)
  Base64String  : payload = "3q2+7w=="  (compact, but opaque to a human editing the file - TOML has no binary type, so one of these has to be chosen)
  Round trip    : payload restored -> DEADBEEF  (the original DEADBEEF - but only because the reader was told which shape to expect; the wire form does not say)
```

**APIs demonstrated.** `TomlSerializerOptions.SpecVersion`, `TomlSpecVersion.V1_0` / `.V1_1`,
`TomlFormatException`, `TomlSerializerOptions.ByteArrayHandling`,
`TomlByteArrayHandling.IntegerArray` / `.Base64String`.

## Layout

```text
Bodu.Text.Toml.Samples.TomlBasics/
  Program.cs                       # runs the scenarios in order
  SampleConsole.cs                 # the What / Why / Expect scenario banner
  AppConfig.cs                     # the shared POCO graph (attributes annotated)
  Data/app-config.toml             # the committed input document
  Scenarios/SerializerRoundTrip.cs
  Scenarios/TemporalKinds.cs
  Scenarios/NamingAndAttributes.cs
  Scenarios/SpecVersionAndBytes.cs
```

## Related

- `Bodu.Text.Toml.Samples.TomlDocuments` — the layers beneath the serializer: the mutable
  `TomlNode` DOM, the read-only `TomlDocument` DOM, and the `Utf8TomlReader`/`Utf8TomlWriter`
  token surface.
- Guides: `docs/guides/serialization/toml/`.
