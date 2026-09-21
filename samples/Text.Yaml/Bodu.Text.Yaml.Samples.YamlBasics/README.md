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
--- YamlSerializer - file to POCO to YAML and back ---
  What   : Deserializes the committed config file into a typed object graph with a nested mapping and a block
           sequence of mappings, reads the values, changes one, serializes back, and deserializes the result again.
  Why    : YAML is the format most configuration is written in and the one with the largest gap between what it can
           express and what a program should accept. Deliberately shaping this serializer like System.Text.Json is
           the point: the options, attributes and naming policies are the ones a .NET developer already knows, and a
           typed model puts a bound on the document rather than accepting whatever the file happens to contain. The
           round trip matters because config tooling usually has to write files back, and a library that reads a
           format but cannot re-emit it leaves the caller hand-building YAML.
  Expect : The nested mapping binds to a class property and the sequence of mappings to a list. After the edit the
           re-emitted document parses back with the changed value and the same number of endpoints, so re-emitting
           preserved both structures rather than flattening them.

  Service   : orders (enabled: True, retries: 3)
  Database  : localhost:5432  (a nested mapping binds to a nested class - YAML structure maps onto object structure)
  Endpoints : public -> https://api.example.com, admin -> https://admin.example.com  (a block sequence of mappings binds to a list of objects)
  Round trip: retries 5, endpoints 2 (values preserved)  (the edit survives and neither structure flattens - config tooling usually has to write the file back)
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
--- Implicit scalar typing and float-to-integer handling ---
  What   : Binds a document of plain and quoted scalars to a loose dictionary and reports the runtime type each one
           resolved to, then binds a non-integral float to an int property under the strict default and again with
           float-to-integer coercion allowed.
  Why    : This is what makes YAML different from every other format here, and the source of most surprises in it.
           An unquoted scalar has no declared type - the spec resolution rules decide whether null, true and 3 are a
           null, a boolean and an integer or just text, and quoting is how an author opts out. That is why this
           library keeps its scalar converters format-local rather than sharing the token-strict ones its siblings
           use: they have to coerce across kinds, which the shared converters cannot express. Number handling is the
           same question one layer up - 3.7 into an int is either an error or a truncation, and choosing truncation
           silently is how a configured value quietly becomes 3.
  Expect : Five plain scalars resolve to five different runtime types with nothing declaring them, while the quoted
           value stays a string despite looking like a number - the quotes are the author type annotation. The
           strict binding then rejects 3.7 rather than truncating, and truncation happens only when asked for
           explicitly.

  name    -> String  : orders
  count   -> Int64   : 3
  ratio   -> Double  : 0.25
  enabled -> Boolean : True
  note    -> null    : (null)
  quoted  -> String  : 42
  (nothing declared these types - the spec resolution rules did, and the quoted value opted out by being quoted)
  Strict             : 3.7 -> int rejected -> The floating-point value '3.7' cannot be deserialized into an integer target without loss of precision.  (the default refuses rather than truncating, because a configured value quietly becoming 3 is the worse failure)
  AllowFloatToInteger: 3.7 -> int accepted -> 3  (truncated toward zero, and only because it was asked for explicitly)
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
--- Sequences to lists and arrays, mappings to dictionaries ---
  What   : Binds a document containing a block sequence, a string-keyed mapping, and a mapping of sequences onto one
           class, then binds a flow sequence to a fixed array and round-trips the whole graph.
  Why    : YAML has exactly two container shapes, and the useful thing about them is that they compose without limit
           - a mapping of sequences of mappings is ordinary YAML, not an extension. The binding rules follow from
           that: a sequence is a list or an array, a mapping is a dictionary or an object, and which one you get is
           decided by the target type rather than by anything in the document. That is why a section whose keys are
           open-ended can bind to a dictionary and one whose shape is fixed to a class, in the same file and on the
           same class.
  Expect : Three different .NET shapes bound from three container expressions, including a nested dictionary of
           lists that required no special handling. The flow sequence binds to an array from the same rules that
           bound the block sequence to a list - block and flow are two spellings of one structure, not two
           structures.

  ports   -> List<int>                     : [8080, 8443]
  weights -> Dictionary<string,int>        : primary=3, backup=1
  zones   -> Dictionary<string,List<string>>: us=[us-east,us-west]; eu=[eu-central]  (containers compose without limit - a mapping of sequences needs no special handling)
  flow sequence -> int[]                   : [8080, 8443, 9090]  (flow and block are two spellings of one structure - the target type decides list or array, not the document)
  round trip: 2 ports, 2 weights, 2 zones (preserved)  (all three shapes survive a re-emit and re-read, nesting included)
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
--- Naming policies, attributes, and enum output ---
  What   : Serializes one POCO under three naming policies, shows a property renamed by attribute and another
           excluded entirely, provokes the missing-required-key failure, then emits an enum as a name and as a
           number.
  Why    : The wire name and the C# name answer to different audiences. YAML files are hand-edited, so their keys
           follow the format conventions rather than .NET ones, and a policy settles that for a whole type instead
           of an attribute per property. Attributes cover what a convention cannot reach: a key whose spelling is
           fixed by an external contract, a computed property that should never appear in an editable file, and a
           key whose absence is an error rather than a default. The last is the one that earns its place - without
           it a missing service_name deserializes to an empty string and fails somewhere unrelated, long after the
           config was read.
  Expect : One type produces three key spellings with no change to the class. The renamed property appears under its
           wire name, the ignored one is absent entirely, and the missing required key is rejected by name at
           deserialize time. The enum rows show why the string form is the default: the name survives a renumbering
           of the enum, the number does not.

  CamelCase     : jobName: nightly-sync
  SnakeCaseLower: job_name: nightly-sync
  KebabCaseLower: job-name: nightly-sync
  (one class, three key spellings - the convention belongs to the file, not to the C# type)
  [PropertyName] : endpoints write 'url:' -> True  (expected True - the attribute overrides the policy where an external contract fixes the spelling)
  [Ignore]       : 'display_label' absent -> True  (expected True - a computed property in an editable file looks like something a reader may change)
  [Required]     : missing key rejected -> The required member 'service_name' of type 'Bodu.Text.Yaml.Samples.YamlBasics.AppConfig' was not present in the input.  (without this the field would bind to an empty string and fail somewhere unrelated, much later)
  enum as string : run_day: Wednesday
  enum as number : run_day: 3  (why the string form is the default: the name survives a renumbering of the enum, the number does not)
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
--- Spec version, scalar styles, duplicate keys and merge keys ---
  What   : Resolves the same plain scalar under both YAML spec versions, emits two string values and shows which one
           the writer quotes, rejects a repeated mapping key and then resolves it under both lenient policies, and
           expands a merge key before disabling it.
  Why    : Each of these is a place where YAML is ambiguous enough that a library has to take a position. The spec
           version is the famous one - under YAML 1.1 the plain scalar no resolves to a boolean, which is why a
           country code for Norway can become false in a config file; 1.2 narrows booleans to true and false, so it
           is the default here. Quoting on output is the same problem in reverse: the writer has to quote any string
           that would resolve to a non-string on the way back in, or the document does not round-trip. Duplicate
           keys are undefined behaviour in practice - silently keeping one is how a config change appears to do
           nothing - so the default is to refuse and the lenient modes are explicit about which occurrence wins.
           Merge keys are a genuine feature, but one that makes a mapping depend on an anchor defined elsewhere, so
           it can be turned off.
  Expect : The same document yields a string under 1.2 and a boolean under 1.1 - one line of config, two meanings,
           decided by a setting rather than by the file. The writer leaves an unambiguous value plain and quotes the
           one that would otherwise come back as a boolean. The duplicate key is an error by default and resolves
           differently under each lenient policy, and disabling the merge key leaves it visible as an ordinary key
           rather than expanding it.

  V1_2 (default): 'yes' -> String (yes)  (1.2 narrows booleans to true/false, so the default does not reinterpret words an author wrote as text)
  V1_1          : 'yes' -> Boolean (True)  (the broader 1.1 schema - the same rule that turns the country code NO into false)
  style: plain: hello
  style: quoted: "true"
  (the writer quotes exactly what has to be quoted: leaving "true" plain would read back as a boolean)
  Throw (default): duplicate rejected -> The mapping key is already defined.  (silently keeping one occurrence is how an edit to a config file appears to do nothing at all)
  UseFirst       : port -> 80  (deterministic, and explicit about which occurrence wins rather than leaving it to parse order)
  UseLast        : port -> 443
  Expand (default): service keys -> [retries, timeout] (retries 5)  (timeout came from the anchor, and the local retries overrode the merged one)
  Disabled        : service keys -> [<<, retries] ('<<' retained)  (the merge key stays an ordinary key - the opt-out for documents where a mapping should not depend on an anchor elsewhere)
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
  SampleConsole.cs                 # the What / Why / Expect scenario banner
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
