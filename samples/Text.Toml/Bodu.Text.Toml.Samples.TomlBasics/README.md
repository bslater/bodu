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
Service   : orders (enabled: True, retries: 3)
Database  : localhost:5432
Endpoints : public -> https://api.example.com, admin -> https://admin.example.com
Round trip: retries 5, endpoints 2 (values preserved)
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
released_on        -> DateOnly       : 01/15/2026
maintenance_window -> TimeOnly       : 03:30
build_stamp        -> DateTimeOffset : 01/15/2026 08:30:00 +10:00 (offset 10:00:00)
  wire: released_on = 2026-01-15
  wire: maintenance_window = 03:30:00
  wire: build_stamp = 2026-01-15T08:30:00+10:00
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
CamelCase     : jobName = "nightly-sync"
SnakeCaseLower: job_name = "nightly-sync"
KebabCaseLower: job-name = "nightly-sync"
[TomlPropertyName] : endpoints write 'url =' -> True
[TomlIgnore]       : 'display_label' absent  -> True
[TomlRequired]     : missing key rejected   -> Required member 'service_name' was not present ...
enum default       : run_day = "Wednesday"
enum policy-cased  : run_day = "wednesday"
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
V1_0 (default): rejected -> The escape sequence is not valid.
V1_1          : accepted -> greeting = "café"
IntegerArray  : payload = [222, 173, 190, 239]
Base64String  : payload = "3q2+7w=="
Round trip    : payload restored -> DEADBEEF
```

**APIs demonstrated.** `TomlSerializerOptions.SpecVersion`, `TomlSpecVersion.V1_0` / `.V1_1`,
`TomlFormatException`, `TomlSerializerOptions.ByteArrayHandling`,
`TomlByteArrayHandling.IntegerArray` / `.Base64String`.

## Layout

```text
Bodu.Text.Toml.Samples.TomlBasics/
  Program.cs                       # runs the scenarios in order
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
