---
title: Writer options and DOM options
---

# Writer options and DOM options

The `<Format>SerializerOptions` classes are not the only option types in the text libraries. Each format also has small **option structs** for the layers beneath the serializer: a `<Format>WriterOptions` passed to the `Utf8<Format>Writer` constructor, and — for the formats whose DOMs take one — a `<Format>NodeOptions` for the mutable DOM and a `<Format>DocumentOptions` (or reader options) for the read-only DOM's `Parse`. They are value types, the zero value is the default, and none of them freezes: a struct is copied into the writer or parser at construction and cannot be observed changing afterwards.

This guide lists every member of every such struct with its default, and shows the effect of each knob on real bytes. The serializer-level lifecycle — freezing, caching, presets — is covered in [Serializer options: freezing, caching, and thread safety](../serialization/options-and-lifetime.md); the *reader* dialect knobs (`DelimitedReaderOptions`, `DotEnvReaderOptions`, `IniReaderOptions`) are tabulated on the [parser policies](../../docs/formats/parser-policies.md) page and only cross-referenced here.

## The option structs at a glance

| Struct | Passed to | Members |
|---|---|---|
| <xref:Bodu.Text.Delimited.Writer.DelimitedWriterOptions> | `Utf8DelimitedWriter(output, options)` | `Delimiter`, `Quote`, `NoHeader` |
| <xref:Bodu.Text.DotEnv.Writer.DotEnvWriterOptions> | `Utf8DotEnvWriter(output, options)` | `WriteExportPrefix` |
| <xref:Bodu.Text.Ini.Writer.IniWriterOptions> | `Utf8IniWriter(output, options)` | `CommentPrefix` |
| <xref:Bodu.Text.Bencode.Writer.BencodeWriterOptions> | `Utf8BencodeWriter(output, options)` | `MaxDepth`, `AllowMultipleRootValues` |
| <xref:Bodu.Text.Toml.Writer.TomlWriterOptions> | `Utf8TomlWriter(output, options)` | `MaxDepth` (`SpecVersion` is obsolete) |
| <xref:Bodu.Text.Yaml.Writer.YamlWriterOptions> | `Utf8YamlWriter(output, options)` | `IndentSize`, `MaxDepth`, `NewLine` |
| <xref:Bodu.Text.Bencode.Nodes.BencodeNodeOptions> | `BencodeNode.Parse(data, options[, documentOptions])`, `new BencodeObject(options)` | `PropertyNameCaseInsensitive` |
| <xref:Bodu.Text.Bencode.Document.BencodeDocumentOptions> | `BencodeDocument.Parse(data, options)` | `MaxDepth`, `AllowUnsortedKeys`, `AllowDuplicateKeys` |
| <xref:Bodu.Text.Toml.Nodes.TomlNodeOptions> | `TomlNode.Parse(utf8, options)`, `new TomlObject(options)` | `PropertyNameCaseInsensitive` |
| <xref:Bodu.Text.Toml.Document.TomlDocumentOptions> | `TomlDocument.Parse(text, options)` | `SpecVersion`, `MaxDepth` |
| <xref:Bodu.Text.Yaml.Document.YamlDocumentOptions> | `YamlDocument.Parse(text, options)`, `ParseAllDocuments(text, options)` | `SpecVersion`, `DuplicateKeyBehavior`, `MergeKeyBehavior`, `MaxDepth` |
| <xref:Bodu.Text.Ini.IniDocumentOptions> | `IniNode.Parse(utf8, readerOptions, documentOptions)`, `IniDocument.Parse(…)`, `new IniDocumentReader(…)` | `DuplicateSectionBehavior`, `DuplicateKeyBehavior` |

Delimited and DotEnv have no DOM option struct of their own: `DelimitedNode.Parse` / `DelimitedDocument.Parse` take a <xref:Bodu.Text.Delimited.Reader.DelimitedReaderOptions>, and `DotEnvNode.Parse` / `DotEnvDocument.Parse` take a <xref:Bodu.Text.DotEnv.Reader.DotEnvReaderOptions>. YAML's `YamlNode.Parse(string)` takes no options; the reader's <xref:Bodu.Text.Yaml.Reader.YamlReaderOptions> mirrors `YamlDocumentOptions` member for member.

The line-format structs are `readonly struct`s with `init` accessors and a static `Default`; the Bencode, TOML, and YAML structs are ordinary mutable structs whose `default` is the default configuration.

## Pattern 1 — Delimited writer: dialect and header

| Member | Type | Default | Effect |
|---|---|---|---|
| `Delimiter` | `char` | `'\0'` → `,` | The field separator. |
| `Quote` | `char` | `'\0'` → `"` | The quoting character; a field containing the delimiter, the quote, or a line break is wrapped in it, with embedded quotes doubled. |
| `NoHeader` | `bool` | `false` | Suppress the header row that the first object record would otherwise emit. |

Records written as objects (`WriteStartObject` / `WritePropertyName` / `WriteString`) emit a header from the first record's property names unless `NoHeader` is set. A tab-separated, header-less file with single-quote quoting:

```csharp
using System.Buffers;
using Bodu.Text.Delimited.Writer;

var buffer = new ArrayBufferWriter<byte>();
var writer = new Utf8DelimitedWriter(buffer, new DelimitedWriterOptions { Delimiter = '\t', Quote = '\'', NoHeader = true });
writer.WriteStartArray();
writer.WriteStartObject();
writer.WritePropertyName("symbol"); writer.WriteString("MSFT");
writer.WritePropertyName("note");   writer.WriteString("tab\there");
writer.WriteEndObject();
writer.WriteEndArray();
writer.Flush();
// MSFT\t'tab\there'\r\n
```

The value containing a tab is quoted with `'` because the tab is now the delimiter. The same records with the default options produce a header and RFC 4180 double-quote doubling:

```text
symbol,note
MSFT,"say ""hi"", now"
```

Records end with `\r\n` in both dialects. `DelimitedSerializerOptions` exposes the same three members and forwards them to this struct (and to the reader options) on your behalf.

## Pattern 2 — DotEnv writer: the `export` prefix

| Member | Type | Default | Effect |
|---|---|---|---|
| `WriteExportPrefix` | `bool` | `false` | Prefix every key written through `WritePropertyName(string)` with `export `. |

The option sets the default for the one-argument `WritePropertyName`; the two-argument overload `WritePropertyName(name, export)` decides per key and ignores the option:

```csharp
using Bodu.Text.DotEnv.Writer;

var buffer = new ArrayBufferWriter<byte>();
var writer = new Utf8DotEnvWriter(buffer, new DotEnvWriterOptions { WriteExportPrefix = true });
writer.WriteStartObject();
writer.WriteComment("generated");
writer.WritePropertyName("API_URL");               writer.WriteString("https://example.test");
writer.WritePropertyName("SECRET", export: false); writer.WriteString("s3cret #1");
writer.WriteEndObject();
writer.Flush();
// #generated
// export API_URL=https://example.test
// SECRET="s3cret #1"
```

The value containing ` #` is double-quoted automatically, because unquoted it would read back as `s3cret` followed by a comment. `DotEnvSerializerOptions.WriteExportPrefix` is the serializer-level form of the same flag; the mutable `DotEnvObject` DOM preserves each entry's own export flag instead.

## Pattern 3 — INI writer: the comment prefix

| Member | Type | Default | Effect |
|---|---|---|---|
| `CommentPrefix` | `char` | `'\0'` → `;` | The character `WriteComment` (and the DOM's comment trivia) is emitted with. |

```csharp
using Bodu.Text.Ini.Writer;

var buffer = new ArrayBufferWriter<byte>();
var writer = new Utf8IniWriter(buffer, new IniWriterOptions { CommentPrefix = '#' });
writer.WriteComment("generated");
writer.WritePropertyName("mode"); writer.WriteString("fast");
writer.WriteSectionHeader("server");
writer.WritePropertyName("port"); writer.WriteString("8080");
writer.Flush();
// #generated
// mode=fast
// [server]
// port=8080
```

Both `;` and `#` are comment starters on the read side by default, so either prefix round-trips; choose `#` only when a downstream reader with `IniReaderOptions.DisallowHashComments` will *not* be consuming the file.

## Pattern 4 — Bencode writer: depth and multiple roots

| Member | Type | Default | Effect |
|---|---|---|---|
| `MaxDepth` | `int` | `0` → 64 | The deepest container nesting the writer opens before throwing `BencodeSerializationException`. |
| `AllowMultipleRootValues` | `bool` | `false` | Permit a second complete value after the first root has closed. |

A Bencode document is single-valued; the writer enforces that unless told otherwise:

```csharp
using Bodu.Text.Bencode.Writer;

var buffer = new ArrayBufferWriter<byte>();
var writer = new Utf8BencodeWriter(buffer);
writer.WriteInteger(1);
writer.WriteInteger(2);
// → throws InvalidOperationException: A complete root value has already been written; a Bencode document is single-valued unless AllowMultipleRootValues is set.
```

```csharp
var writer = new Utf8BencodeWriter(buffer, new BencodeWriterOptions { AllowMultipleRootValues = true });
writer.WriteInteger(1);
writer.WriteString("two");
// i1e3:two
```

Multiple roots are how a stream of bencoded messages (a DHT or peer-wire log, for example) is written to one buffer. The depth guard is the same family of error the serializer raises:

```csharp
var writer = new Utf8BencodeWriter(buffer, new BencodeWriterOptions { MaxDepth = 1 });
writer.WriteStartList();
writer.WriteStartList();
// → throws BencodeSerializationException: The maximum write depth of 1 has been exceeded.
```

## Pattern 5 — TOML writer: depth

| Member | Type | Default | Effect |
|---|---|---|---|
| `MaxDepth` | `int` | `0` → 64 | Deepest table/array nesting; larger values are clamped to the library's absolute limit. Exceeding it throws `TomlSerializationException`. |
| `SpecVersion` | `TomlSpecVersion` | — | **Obsolete.** The writer emits output valid under both TOML v1.0.0 and v1.1.0, so the member has no effect and will be removed. |

```csharp
using Bodu.Text.Toml.Writer;

var buffer = new ArrayBufferWriter<byte>();
var writer = new Utf8TomlWriter(buffer, new TomlWriterOptions { MaxDepth = 1 });
writer.WriteStartTable();
writer.WritePropertyName("server");
writer.WriteStartTable();
writer.WritePropertyName("tls");
writer.WriteStartTable();
// → throws TomlSerializationException: The maximum write depth of 1 has been exceeded.
```

With `MaxDepth = 2` the same two-level document renders — note that the TOML writer delivers its bytes when the *root table closes*, because it must decide between inline and header-defined tables with the whole document in hand:

```csharp
var writer = new Utf8TomlWriter(buffer, new TomlWriterOptions { MaxDepth = 2 });
writer.WriteStartTable();
writer.WritePropertyName("server");
writer.WriteStartTable();
writer.WritePropertyName("port"); writer.WriteInteger(8080);
writer.WriteEndTable();
writer.WriteEndTable();
// [server]
// port = 8080
```

## Pattern 6 — YAML writer: indentation, line endings, depth

| Member | Type | Default | Effect |
|---|---|---|---|
| `IndentSize` | `int` | `0` → 2 | Spaces per nesting level, 1–16. |
| `NewLine` | `string?` | `null` → `"\n"` | The line terminator; only `"\n"` and `"\r\n"` are accepted. |
| `MaxDepth` | `int` | `0` → 64 | Deepest mapping/sequence nesting; larger values are clamped to the absolute limit. |

```csharp
using Bodu.Text.Yaml.Writer;

var buffer = new ArrayBufferWriter<byte>();
var writer = new Utf8YamlWriter(buffer, new YamlWriterOptions { IndentSize = 4, NewLine = "\r\n" });
writer.WriteStartMapping();
writer.WritePropertyName("server");
writer.WriteStartMapping();
writer.WritePropertyName("port"); writer.WriteInteger(8080);
writer.WritePropertyName("hosts");
writer.WriteStartSequence();
writer.WriteString("a"); writer.WriteString("b");
writer.WriteEndSequence();
writer.WriteEndMapping();
writer.WriteEndMapping();
// server:\r\n
//     port: 8080\r\n
//     hosts:\r\n
//         - a\r\n
//         - b\r\n
```

The default options render the same document at two spaces with `\n`. Unlike the other writer structs, `YamlWriterOptions` is **validated at construction** rather than at the point of use:

```csharp
new Utf8YamlWriter(buffer, new YamlWriterOptions { NewLine = "\r" });
// → throws ArgumentException: The writer newline must be null, a line feed, or a carriage-return line feed. (Parameter 'NewLine')

new Utf8YamlWriter(buffer, new YamlWriterOptions { IndentSize = 32 });
// → throws ArgumentOutOfRangeException: The writer indentation size must be between 1 and 16. (Parameter 'IndentSize')
```

Opening a container past `MaxDepth` throws `InvalidOperationException` (*The maximum write depth of 1 has been exceeded.*) from the writer; the same limit reached through `YamlSerializer` — which uses `YamlSerializerOptions.MaxDepth` and the default writer options — surfaces as `YamlSerializationException`. `YamlSerializer` does not expose the writer options, so serializer output is always two-space, `\n`-terminated YAML; drive `Utf8YamlWriter` directly (or call a node's `WriteTo(writer)`) when you need another layout.

## Pattern 7 — Bencode DOMs: key case and key discipline

| Struct | Member | Default | Effect |
|---|---|---|---|
| `BencodeNodeOptions` | `PropertyNameCaseInsensitive` | `false` | Key lookups on the `BencodeObject` (and its indexer) ignore case. |
| `BencodeDocumentOptions` | `MaxDepth` | `0` → 64 | Deepest nesting the parser accepts; exceeding it throws `BencodeFormatException`. |
| `BencodeDocumentOptions` | `AllowUnsortedKeys` | `false` | Accept dictionaries whose keys are not in raw byte order. |
| `BencodeDocumentOptions` | `AllowDuplicateKeys` | `false` | Accept a repeated key; the **first** occurrence is kept. |

The node options govern *lookup*; the document options govern *parsing*, and `BencodeNode.Parse` has an overload that takes both so a mutable tree can be built from lenient input:

```csharp
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Nodes;

BencodeNode node = BencodeNode.Parse("d4:NAME3:apie"u8, new BencodeNodeOptions { PropertyNameCaseInsensitive = true })!;
string name = node["name"]!.GetValue<string>();      // api
BencodeNode? missing = BencodeNode.Parse("d4:NAME3:apie"u8)!["name"];
// → throws KeyNotFoundException — the default lookup is case-sensitive

BencodeDocument.Parse("d1:bi1e1:ai2ee"u8);
// → throws BencodeFormatException: Bencoded dictionary keys must be sorted by raw byte order.
using BencodeDocument unsorted = BencodeDocument.Parse("d1:bi1e1:ai2ee"u8, new BencodeDocumentOptions { AllowUnsortedKeys = true });
long a = unsorted.RootElement.GetProperty("a").GetInt64();   // 2

BencodeDocument.Parse("d1:ai1e1:ai2ee"u8);
// → throws BencodeFormatException: Bencoded dictionary keys must be unique.
using BencodeDocument dup = BencodeDocument.Parse("d1:ai1e1:ai2ee"u8, new BencodeDocumentOptions { AllowDuplicateKeys = true });
long firstA = dup.RootElement.GetProperty("a").GetInt64();   // 1 — first wins

BencodeDocument.Parse("lli1eee"u8, new BencodeDocumentOptions { MaxDepth = 1 });
// → throws BencodeFormatException: Bencoded value nesting exceeds the maximum permitted depth of 1.

long viaNode = BencodeNode.Parse("d1:bi1e1:ai2ee"u8, default, new BencodeDocumentOptions { AllowUnsortedKeys = true })!["a"]!.GetValue<long>();   // 2
```

<xref:Bodu.Text.Bencode.Reader.BencodeReaderOptions> carries the same three parsing members for the token reader, and `BencodeSerializerOptions` exposes `AllowUnsortedKeys` / `AllowDuplicateKeys` / `PropertyNameCaseInsensitive` (the last defaulting to `true` at the serializer level) for typed reads.

## Pattern 8 — TOML DOMs: key case, spec version, depth

| Struct | Member | Default | Effect |
|---|---|---|---|
| `TomlNodeOptions` | `PropertyNameCaseInsensitive` | `false` | Key lookups on the `TomlObject` ignore case. |
| `TomlDocumentOptions` | `SpecVersion` | `V1_0` | Parse under TOML v1.0.0 or v1.1.0 (v1.1.0 adds multi-line inline tables, trailing commas, and second-precision omission). |
| `TomlDocumentOptions` | `MaxDepth` | `0` → 64 | Deepest nesting the parser accepts; exceeding it throws `TomlFormatException`. |

```csharp
using Bodu.Text.Toml;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Nodes;

TomlNode tnode = TomlNode.Parse("Port = 8080"u8, new TomlNodeOptions { PropertyNameCaseInsensitive = true })!;
long port = tnode["port"]!.GetValue<long>();       // 8080
bool found = TomlNode.Parse("Port = 8080"u8)!.AsObject().TryGetPropertyValue("port", out _);   // false — case-sensitive by default

TomlDocument.Parse("a.b.c = 1", new TomlDocumentOptions { MaxDepth = 1 });
// → throws TomlFormatException: TOML value nesting exceeds the maximum permitted depth of 1.

TomlDocument.Parse("x = {\n a = 1\n}", new TomlDocumentOptions { SpecVersion = TomlSpecVersion.V1_0 });
// → throws TomlFormatException: Expected a key.
using TomlDocument d = TomlDocument.Parse("x = {\n a = 1\n}", new TomlDocumentOptions { SpecVersion = TomlSpecVersion.V1_1 });
long inner = d.RootElement.GetProperty("x").GetProperty("a").GetInt64();   // 1
```

<xref:Bodu.Text.Toml.Reader.TomlReaderOptions> carries the same `SpecVersion` / `MaxDepth` pair for the token reader; `TomlSerializerOptions` exposes both plus `PropertyNameCaseInsensitive`.

## Pattern 9 — YAML DOM: duplicate keys, merge keys, spec version, depth

| Member | Default | Values | Effect |
|---|---|---|---|
| `DuplicateKeyBehavior` | `Throw` | `Throw` · `UseFirst` · `UseLast` | What a repeated mapping key means. |
| `MergeKeyBehavior` | `Expand` | `Expand` · `Disabled` · `PreserveAsNormalKey` | Whether `<<: *alias` merges the referenced mapping (existing keys win) or is retained as an ordinary key named `<<`. `Disabled` and `PreserveAsNormalKey` produce the same tree; the second spells out the intent. |
| `SpecVersion` | `V1_2` | `V1_2` · `V1_1` | The core schema: under 1.2 only `true` / `false` are booleans; 1.1 restores `yes` / `no` / `on` / `off`. |
| `MaxDepth` | `0` → 64 | | Deepest node nesting the parser accepts; exceeding it throws `YamlFormatException`. |

```csharp
using Bodu.Text.Yaml;
using Bodu.Text.Yaml.Document;

YamlDocument.Parse("a: 1\na: 2");
// → throws YamlFormatException: The mapping key is already defined.
using YamlDocument last = YamlDocument.Parse("a: 1\na: 2", new YamlDocumentOptions { DuplicateKeyBehavior = YamlDuplicateKeyBehavior.UseLast });
long value = last.RootElement.GetProperty("a").GetInt64();    // 2

const string merge = "base: &b\n  x: 1\nchild:\n  <<: *b\n  y: 2\n";
using YamlDocument expanded = YamlDocument.Parse(merge);
bool merged = expanded.RootElement.GetProperty("child").TryGetProperty("x", out _);     // true — merged in
using YamlDocument kept = YamlDocument.Parse(merge, new YamlDocumentOptions { MergeKeyBehavior = YamlMergeKeyBehavior.PreserveAsNormalKey });
bool hasMergeKey = kept.RootElement.GetProperty("child").TryGetProperty("<<", out _);   // true — an ordinary key
bool hasX = kept.RootElement.GetProperty("child").TryGetProperty("x", out _);            // false

using YamlDocument core = YamlDocument.Parse("legacy: yes");
YamlValueKind coreKind = core.RootElement.GetProperty("legacy").ValueKind;   // String
using YamlDocument v11 = YamlDocument.Parse("legacy: yes", new YamlDocumentOptions { SpecVersion = YamlSpecVersion.V1_1 });
YamlValueKind v11Kind = v11.RootElement.GetProperty("legacy").ValueKind;     // Boolean

YamlDocument.Parse("a:\n  b: 1", new YamlDocumentOptions { MaxDepth = 1 });
// → throws YamlFormatException: YAML node nesting exceeds the maximum permitted depth of 1.
```

`YamlReaderOptions` has the same four members for `Utf8YamlReader`, and `YamlSerializerOptions` exposes all four for typed reads. `YamlNode.Parse(string)` takes no options and parses with the defaults.

## Pattern 10 — INI DOM: duplicate policies and reader dialect

| Struct | Member | Default | Values |
|---|---|---|---|
| `IniDocumentOptions` | `DuplicateSectionBehavior` | `Merge` | `Merge` (later `[section]` blocks append to the first) · `Disallowed` |
| `IniDocumentOptions` | `DuplicateKeyBehavior` | `LastWins` | `LastWins` · `FirstWins` · `Disallowed` |
| `IniReaderOptions` | `DisallowHashComments` | `false` | Only `;` starts a comment; a `#` line becomes a malformed entry. |
| `IniReaderOptions` | `SkipComments` | `false` | Drop comment lines from the token stream (and so from the DOM's comment trivia). |

The `Parse` overloads on both INI DOMs take the reader options and the document options together:

```csharp
using Bodu.Text.Ini;
using Bodu.Text.Ini.Nodes;
using Bodu.Text.Ini.Reader;

const string ini = "# hash comment\nname=a\nname=b\n[s]\nk=1\n[s]\nk=2\nj=3\n";

IniObject root = IniNode.Parse(ini);
string name = root["name"].AsValue().Value;              // b  — LastWins
string k = root["s"].AsObject()["k"].AsValue().Value;     // 2
string j = root["s"].AsObject()["j"].AsValue().Value;     // 3  — the second [s] block merged in

IniObject first = IniNode.Parse(Encoding.UTF8.GetBytes(ini), IniReaderOptions.Default,
    new IniDocumentOptions { DuplicateKeyBehavior = IniDuplicateKeyBehavior.FirstWins });
string firstName = first["name"].AsValue().Value;              // a
string firstK = first["s"].AsObject()["k"].AsValue().Value;     // 1

IniNode.Parse(Encoding.UTF8.GetBytes(ini), IniReaderOptions.Default,
    new IniDocumentOptions { DuplicateKeyBehavior = IniDuplicateKeyBehavior.Disallowed });
// → throws IniFormatException: The INI section '' defines the key 'name' more than once.

IniNode.Parse(Encoding.UTF8.GetBytes(ini), IniReaderOptions.Default,
    new IniDocumentOptions { DuplicateSectionBehavior = IniDuplicateSectionBehavior.Disallowed });
// → throws IniFormatException: The INI document defines the section 's' more than once.

IniNode.Parse(Encoding.UTF8.GetBytes(ini), new IniReaderOptions { DisallowHashComments = true }, IniDocumentOptions.Default);
// → throws IniFormatException: Malformed INI entry on line 1; expected 'key=value'.
```

`IniSerializerOptions` exposes the two duplicate policies directly (its `Strict` preset sets both to `Disallowed`) and forwards them to `IniDocumentOptions`.

## Serializer option ↔ struct member

Where a serializer option and a struct member control the same thing, the serializer forwards its value to the struct; set it once at the serializer level for typed use, or on the struct when driving the writer or DOM directly.

| Serializer option | Forwarded to |
|---|---|
| `DelimitedSerializerOptions.Delimiter` / `Quote` / `NoHeader` | `DelimitedWriterOptions` and `DelimitedReaderOptions` |
| `DotEnvSerializerOptions.WriteExportPrefix` | `DotEnvWriterOptions.WriteExportPrefix` |
| `IniSerializerOptions.DuplicateSectionBehavior` / `DuplicateKeyBehavior` | `IniDocumentOptions` |
| `BencodeSerializerOptions.AllowUnsortedKeys` / `AllowDuplicateKeys` / `MaxDepth` | `BencodeReaderOptions` (reads); `MaxDepth` also governs the write |
| `BencodeSerializerOptions.PropertyNameCaseInsensitive` | member matching — the same rule `BencodeNodeOptions` applies to DOM lookups |
| `TomlSerializerOptions.SpecVersion` / `MaxDepth` | `TomlReaderOptions` (reads); `MaxDepth` also governs the write |
| `YamlSerializerOptions.SpecVersion` / `DuplicateKeyBehavior` / `MergeKeyBehavior` / `MaxDepth` | `YamlReaderOptions` (reads); `MaxDepth` also governs the write |

`IniWriterOptions.CommentPrefix` and the three `YamlWriterOptions` members have no serializer-level counterpart.

## Where to go next

- [Parser policies](../../docs/formats/parser-policies.md) — the reader-option structs (`DelimitedReaderOptions`, `DotEnvReaderOptions`, `IniReaderOptions`) in full.
- [Errors across the line formats](error-handling.md) — the exceptions the strict defaults above raise, and how to catch them.
- [Streams and token-level I/O](streaming.md) — driving the `Utf8*Writer` types directly, which is where the writer structs apply.
- [Serializer options: freezing, caching, and thread safety](../serialization/options-and-lifetime.md) — the `<Format>SerializerOptions` lifecycle.
- The per-format guides — [Delimited](delimited.md), [DotEnv](dotenv.md), [INI](ini.md) — and the serializer DOM walk-throughs in [Using TOML](../serialization/toml/using.md), [Using Bencode](../serialization/bencode/using.md), and [Using YAML](../serialization/yaml/using.md).
- [Runnable samples](../../samples/formats.md), the [line-format guides hub](index.md), and the [Text & Serialization guides](../topics/text-and-serialization.md).
