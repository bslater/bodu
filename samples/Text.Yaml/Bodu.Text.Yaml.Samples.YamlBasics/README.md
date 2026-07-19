# Bodu.Text.Yaml.Samples.YamlBasics

The starting point for `Bodu.Text.Yaml`: the `YamlSerializer` POCO surface, shaped after
`System.Text.Json`. Five scenarios cover the serialize/deserialize loop, YAML's implicit scalar
typing, how sequences and mappings bind to .NET collections, how wire names are chosen (naming
policies vs the attribute family), and the parse/emit knobs (`SpecVersion`, scalar styles,
`DuplicateKeyBehavior`, `MergeKeyBehavior`).

Everything runs offline against the committed `Data/app-config.yaml` — no network, no
machine-specific state, deterministic output.

```bash
dotnet run --project samples/Text.Yaml/Bodu.Text.Yaml.Samples.YamlBasics
```

## Scenario 1 — SerializerRoundTrip

**Intent.** Show the core workflow every consumer starts with: a YAML file on disk becomes a
typed object graph in one call, and the graph becomes YAML again in one call — the
`System.Text.Json` workflow (`Deserialize<T>` / `Serialize<T>`), for YAML.

**What it does.** Reads `Data/app-config.yaml` (a scalar section, a nested `database` mapping,
and an `endpoints` block sequence of mappings) and deserializes it into the `AppConfig` POCO
using the `SnakeCaseLower` naming policy, so `service_name` binds to `ServiceName` without any
attributes. It then mutates `MaxRetries`, serializes the graph back to YAML text, and
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

**APIs demonstrated.** `YamlSerializer.Deserialize<T>(string, YamlSerializerOptions)`,
`YamlSerializer.Serialize<T>`, `YamlSerializerOptions.PropertyNamingPolicy`,
`NamingPolicy.SnakeCaseLower`, nested mappings and block sequences binding to POCOs and
`List<T>`.

## Scenario 2 — ScalarKinds

**Intent.** Demonstrate YAML's defining scalar feature over JSON: *implicit typing*. An
unquoted (plain) scalar is resolved to null, boolean, integer, float, or string by the active
`YamlSpecVersion`, and the serializer surfaces each as the matching .NET runtime type. Quoting
forces the string interpretation.

**What it does.** Binds a document of plain and quoted scalars into a loose
`Dictionary<string, object?>` and prints the resolved runtime type of each value — showing
that the quoted `"42"` stays a `String` while the plain `42` resolves to `Int64`. It then
demonstrates the `YamlNumberHandling` knob: binding the non-integral float `3.7` to an `int`
member is rejected under `Strict` (the default) and truncated toward zero under
`AllowFloatToInteger`.

**What to expect.** Six scalars mapped to their .NET types (`String`, `Int64`, `Double`,
`Boolean`, `null`, and the quoted `String`), then the two number-handling outcomes:

```text
  name    -> String  : orders
  count   -> Int64   : 3
  ratio   -> Double  : 0.25
  enabled -> Boolean : True
  note    -> null    : (null)
  quoted  -> String  : 42
  Strict             : 3.7 -> int rejected -> The floating-point value '3.7' cannot be deserialized into an integer target without loss of precision.
  AllowFloatToInteger: 3.7 -> int accepted -> 3
```

**APIs demonstrated.** Implicit typing to `object` (`string` / `long` / `double` / `bool` /
`null`), quoted-scalar string preservation, `YamlSerializerOptions.NumberHandling`,
`YamlNumberHandling.Strict` / `.AllowFloatToInteger`, `YamlSerializationException`.

## Scenario 3 — CollectionsAndDictionaries

**Intent.** Show how YAML's two container shapes bind to .NET collections: a sequence maps to
`List<T>` and arrays, a mapping maps to `Dictionary<TKey, TValue>`, and the two nest freely.

**What it does.** Deserializes a document combining a block sequence (`ports`), a
string-keyed mapping (`weights`), and a nested mapping of sequences (`zones`) into a POCO with
`List<int>`, `Dictionary<string, int>`, and `Dictionary<string, List<string>>` members. It
also binds a *flow* sequence (`[8080, 8443, 9090]`) to an `int[]`, then round-trips the whole
graph back through YAML.

**What to expect.** Each container reported with the collection it bound to, the flow-sequence
array, and a round-trip line confirming the counts survived:

```text
  ports   -> List<int>                     : [8080, 8443]
  weights -> Dictionary<string,int>        : primary=3, backup=1
  zones   -> Dictionary<string,List<string>>: us=[us-east,us-west]; eu=[eu-central]
  flow sequence -> int[]                   : [8080, 8443, 9090]
  round trip: 2 ports, 2 weights, 2 zones (preserved)
```

**APIs demonstrated.** Block and flow sequences binding to `List<T>` / `T[]`, mappings binding
to `Dictionary<,>`, nested generic collections, `YamlSerializer.Serialize` / `Deserialize`
round-trip.

## Scenario 4 — NamingAndAttributes

**Intent.** Explain the precedence rules for wire names: a naming policy maps every property
by convention; the attribute family overrides it per member. Consumers should reach for the
policy first and attributes only for the exceptions — this scenario shows both layers and where
each wins.

**What it does.** Serializes one small POCO under three policies (`CamelCase`,
`SnakeCaseLower`, `KebabCaseLower`) to show that only key spelling changes. It then round-trips
the shared `AppConfig` and verifies the three attribute behaviours: `Address` writes as `url`
(`[PropertyName]` beats the policy), the computed `DisplayLabel` never reaches the wire
(`[Ignore]`), and deserializing a document missing `service_name` throws
`YamlSerializationException` up front (`[Required]`) instead of silently yielding an empty
string. Finally it contrasts enum output: the default emits the .NET member name, while
`WriteEnumsAsStrings = false` emits the underlying numeric value.

**What to expect.** The same first property spelled three ways, two `True` confirmations for
the attribute overrides, the `[Required]` rejection message naming the missing key, and the
enum emitted as `Wednesday` by default vs `3` in numeric mode:

```text
  CamelCase     : jobName: nightly-sync
  SnakeCaseLower: job_name: nightly-sync
  KebabCaseLower: job-name: nightly-sync
  [PropertyName] : endpoints write 'url:' -> True
  [Ignore]       : 'display_label' absent -> True
  [Required]     : missing key rejected -> The required member 'service_name' of type 'Bodu.Text.Yaml.Samples.YamlBasics.AppConfig' was not present in the input.
  enum as string : run_day: Wednesday
  enum as number : run_day: 3
```

**APIs demonstrated.** `NamingPolicy.CamelCase` / `.SnakeCaseLower` / `.KebabCaseLower`,
`[PropertyName]`, `[Ignore]`, `[Required]`, `YamlSerializationException`,
`YamlSerializerOptions.WriteEnumsAsStrings`.

## Scenario 5 — SpecAndStyles

**Intent.** Surface the parse- and emit-level knobs a consumer eventually needs: `SpecVersion`
gates how plain scalars are typed (the YAML 1.1 "Norway problem"), the writer selects a safe
scalar *style* per value, `DuplicateKeyBehavior` resolves a repeated mapping key, and
`MergeKeyBehavior` controls the merge key (`<<`).

**What it does.** Deserializes `enabled: yes` under `V1_2` (the default, where `yes` stays a
`String`) and `V1_1` (where `yes` resolves to `Boolean` true). It serializes a small mapping to
show the writer emitting a plain scalar for `hello` and a double-quoted scalar for `true` (which
would otherwise re-read as a boolean). It parses a document with a duplicate key under `Throw`
(rejected), `UseFirst`, and `UseLast`. Finally it expands an anchored mapping through the merge
key under `Expand` (the default) and leaves `<<` as a literal key under `Disabled`.

**What to expect.** The Norway-problem type flip, the two scalar styles, the duplicate-key
rejection and the two lenient resolutions, and the merged vs retained `<<` key:

```text
  V1_2 (default): 'yes' -> String (yes)
  V1_1          : 'yes' -> Boolean (True)
  style: plain: hello
  style: quoted: "true"
  Throw (default): duplicate rejected -> The mapping key is already defined.
  UseFirst       : port -> 80
  UseLast        : port -> 443
  Expand (default): service keys -> [retries, timeout] (retries 5)
  Disabled        : service keys -> [<<, retries] ('<<' retained)
```

**APIs demonstrated.** `YamlSerializerOptions.SpecVersion`, `YamlSpecVersion.V1_2` / `.V1_1`,
the writer's `YamlScalarStyle` selection (plain vs double-quoted),
`YamlSerializerOptions.DuplicateKeyBehavior`, `YamlDuplicateKeyBehavior.Throw` / `.UseFirst` /
`.UseLast`, `YamlFormatException`, `YamlSerializerOptions.MergeKeyBehavior`,
`YamlMergeKeyBehavior.Expand` / `.Disabled`.

## Layout

```text
Bodu.Text.Yaml.Samples.YamlBasics/
  Program.cs                       # runs the scenarios in order
  AppConfig.cs                     # the shared POCO graph (attributes annotated)
  Data/app-config.yaml             # the committed input document
  Scenarios/SerializerRoundTrip.cs
  Scenarios/ScalarKinds.cs
  Scenarios/CollectionsAndDictionaries.cs
  Scenarios/NamingAndAttributes.cs
  Scenarios/SpecAndStyles.cs
```

## Related

- `Bodu.Text.Yaml.Samples.YamlDocuments` — the layers beneath the serializer: the mutable
  `YamlNode` DOM, the read-only `YamlDocument` DOM, and the `Utf8YamlReader`/`Utf8YamlWriter`
  token surface.
- Guides: `docs/guides/serialization/yaml/`.
