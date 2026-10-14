---
title: Reading untrusted input
---

# Reading untrusted input

An uploaded file, an email attachment, a downloaded feed, or a configuration file someone else wrote can be crafted to make a reader allocate without bound, recurse until the stack overflows, or spin. This page lists, for every Bodu reader that accepts such input, the limits it enforces, their defaults and ceilings, what happens when one trips, and what is left to you.

## What the readers guarantee

- **Malformed input fails as an exception you can catch.** Each reader throws its format exception, which you can catch, and never fails by overflowing the stack, which you cannot: the recursive readers stop at a nesting depth they cannot be configured past. [Errors and exceptions](errors-and-exceptions.md) lists the types.
- **A declared size is checked before it is trusted.** The container readers bound the sizes a file declares against the file or against a configured limit before allocating for them, because a crafted file can declare any size it likes.
- **Nothing is fetched or executed on the input's behalf.** The calendar document parser prohibits DTDs and resolves no external entities, a document's imports reach only the resolver delegate you pass, and plugin code loads only through an explicit trust policy.

## What is left to you

1. **Bound the input's size before you hand it over.** Most readers hold the whole input in memory: the stream overloads of the six text serializers copy the stream into memory before parsing (all but Delimited's record-by-record `DeserializeAsyncEnumerableAsync`), a compound file opened with the default `Buffered` strategy reads the whole file, a `.bcal` pack is read whole, and the `Bodu.Text.Configuration` reader reads each line whole before measuring it. Check the length of a file or upload first, or wrap the stream so that it stops reading at your limit.
2. **Catch the format exceptions, and treat each as a rejection.** A message is for a log, not for the person who supplied the file; see [errors and exceptions](errors-and-exceptions.md).
3. **Tighten the limits to what your application needs.** The defaults suit real files, not the worst case your service can afford. The tables below say which limits you can lower.

## Text formats

| Reader | Limit | Default | Ceiling | When it trips | Documented in |
|---|---|---|---|---|---|
| `Bodu.Text.Toml` (reader, documents, serializer) | `MaxDepth`: nested tables and arrays | 64 | 64; a larger value is clamped | `TomlFormatException` | [TOML options](../serialization/toml/concepts.md#options) |
| `Bodu.Text.Yaml` (reader, documents, serializer) | `MaxDepth`: nested collections | 64 | 64; a larger value is clamped | `YamlFormatException` | [YAML options](../serialization/yaml/concepts.md#options) |
| `Bodu.Text.Yaml` | Nodes after aliases are expanded, which stops "billion laughs" amplification | 10,000,000 | Fixed | `YamlFormatException` | [The YAML reader](../serialization/yaml/concepts.md#the-buffered-reader-and-the-writer) |
| `Bodu.Text.Bencode` (reader, documents, serializer) | `MaxDepth`: nested lists and dictionaries | 64 | 64; a larger value is clamped | `BencodeFormatException` | [Bencode options](../serialization/bencode/concepts.md#options) |
| `Bodu.Text.Delimited`, `Bodu.Text.DotEnv`, `Bodu.Text.Ini` | None: the formats do not nest | - | - | - | - |
| `Bodu.Text.Configuration` | `MaxLineLength` / `MaxKeyLength` on `ConfigurationParseOptions` | 8,192 / 1,024 characters | None | A `LineTooLong` or `KeyTooLong` error diagnostic: a `ConfigurationParseException` in the default `Throw` mode. | [Parse options](../text-configuration/concepts.md#parse-resolve-and-write-options) |
| `Bodu.Text.Configuration` | A section name used as a glob: length, brace nesting, numeric-range expansion | 4,096 characters, 32 levels, 10,000 values | Fixed | `ConfigurationParseException`. A match that runs past its fixed 100 ms timeout counts as no match. | This page |

`DelimitedSerializer.DeserializeAsyncEnumerableAsync` reads a stream in segments and holds only the record in progress, so its memory is bounded by the longest record rather than the document; no record or field has a size limit of its own. Every other stream overload of the six serializers reads the whole stream first.

## Encodings and filters

| Reader | Limit | Default | When it trips | Documented in |
|---|---|---|---|---|
| `Base58`, `Base58Check`, and `Base62` decoding | Encoded length, because the decode is quadratic in it | 65,536 characters | `Decode` throws `FormatException`; `TryDecode` and `IsValid` return `false`. | [Base58](../../guides/text-encoding/base58.md#span-path) |
| `TextFilter` patterns | Pattern length | 4,096 characters | `ArgumentOutOfRangeException` when the pattern is declared or parsed. | [Brace alternation](../../guides/text-filtering/patterns-and-globs.md#brace-alternation) |
| `TextFilter` globs | Brace nesting, and the alternatives one pattern expands to | 32 levels, 10,000 alternatives | `ArgumentException` when the pattern is declared. | [Brace alternation](../../guides/text-filtering/patterns-and-globs.md#brace-alternation) |
| `TextFilter` regular expressions | `TextFilterOptions.RegexMatchTimeout` | 100 ms | Fails safe: a timed-out include does not admit the value, and a timed-out exclude still rejects it. | [When to use a regex](../../guides/text-filtering/patterns-and-globs.md#when-to-use-a-regex-instead) |

A `TextFilter` prefers the linear-time `RegexOptions.NonBacktracking` engine, so the timeout applies only to a pattern that needs the backtracking engine. If your users supply the patterns, keep the timeout.

## Containers and binary formats

The container readers share one model: a **validation level** decides how the reader responds to an inconsistency it can recover from, while the checks that keep the reader itself safe - bounds, allocation-table sizes, the limits below - hold at every level.

| Reader | Limit or setting | Default | When it trips | Documented in |
|---|---|---|---|---|
| `Bodu.IO.Compound` (`CompoundFileOptions`) | `ReadStrategy`: `Buffered` reads the whole file into memory at open, `Streaming` reads sectors on demand from a seekable stream, `Auto` streams a seekable source longer than `MaxBufferedBytes`. A non-seekable source can only be buffered. | `Buffered`; `MaxBufferedBytes` 64 MiB | Not a limit. | [Buffered vs streaming](../../guides/io-compound/streaming-and-buffering.md) |
| `Bodu.IO.Compound` | `ValidationLevel`: `Strict` rejects anything non-conformant, `Compatible` rejects structural corruption and skips a malformed directory entry, `Minimal` recovers what it can | `Compatible` | `CompoundFileFormatException`, with its `CompoundFileError` category. | [Validation level](../io-compound/concepts.md#validation-level) |
| `Bodu.IO.Compound` | A stream whose declared size is larger than its sector chain | - | `StreamChainTooShort` at `Strict` and `Compatible`. At `Minimal` the missing bytes read as zero, up to the container's length or 1 MiB, whichever is larger. | [Validation level](../io-compound/concepts.md#validation-level) |
| `Bodu.Formats.Excel.Binary` | None of its own. It opens the container streaming, from a seekable stream, at `Compatible`, and holds one sheet's records in memory at a time. | - | `ExcelBinaryFormatException` for a malformed record stream; the container's `CompoundFileFormatException` for a file that is not a valid compound file. | [Reading workbooks](../../guides/excel/reading-workbooks.md#error-handling) |
| `Bodu.IO.Pst` (`PstFileOptions`) | `MaxNodeDataLength`: the largest node payload read whole | 256 MiB | `PstFileFormatException`, `Error == PstFileError.LimitExceeded`, at every validation level. | [`PstFileOptions`](../../guides/io-pst/streaming-and-validation.md#pstfileoptions) |
| `Bodu.IO.Pst` | `MaxDataTreeLeaves`: the leaf blocks one node's data tree may reference | 65,536 | The same, checked before any leaf is read. | [`PstFileOptions`](../../guides/io-pst/streaming-and-validation.md#pstfileoptions) |
| `Bodu.IO.Pst` | `ValidationLevel`: `Strict` verifies every page and block checksum and signature, which the other levels skip | `Compatible` | `PstFileFormatException`. | [Validation levels](../../guides/io-pst/streaming-and-validation.md#what-each-validation-level-checks) |
| `Bodu.Formats.Outlook.Msg`, `Bodu.Formats.Outlook.Pst` | `MaxEmbeddedMessageDepth`: how deep `OpenMessage` follows attached messages | 16 | `OutlookMsgFormatException` / `OutlookPstFormatException`, at every validation level. | [Outlook reader options](../../guides/outlook/reader-options-and-limits.md) |
| The Outlook readers | `MaxDecompressedRtfBytes`: the largest RTF body `BodyRtf` produces, since the declared size sits outside the payload's checksum | 64 MiB | The same, before anything is allocated. | [Outlook reader options](../../guides/outlook/reader-options-and-limits.md) |
| The Outlook readers | `MaxInlineAttachmentBytes`: above it, an attachment's payload stays in the container until you stream it | 1 MiB | Never: a threshold, not a rejection. | [Outlook reader options](../../guides/outlook/reader-options-and-limits.md#the-inline-attachment-threshold-in-practice) |

`BiffReader` reads records from a span you supply and allocates nothing for them; a record's length field is 16 bits, so no record can declare more than 65,535 bytes. The [Outlook reader options guide](../../guides/outlook/reader-options-and-limits.md) and the [PST streaming and validation guide](../../guides/io-pst/streaming-and-validation.md) describe every option, including the ones the mail store does not forward to its container.

### Reading a damaged file

The tolerant levels exist for files that are damaged rather than hostile. `CompoundValidationLevel.Minimal` returns what a corrupt container still holds, reading unreachable bytes as zero. For a `.pst`, `OmitUnresolvableValues` leaves out a value whose reference does not resolve instead of failing the object that holds it, and `ValueOmittedCallback` reports each omission, so you can tell a salvaged message from a clean one. Use these for recovery tools, not as the default for input you do not trust.

## Calendars

| Input | Protection | When it trips | Documented in |
|---|---|---|---|
| A notable-date XML document | DTDs prohibited, no external resolver, at most 64 Mi characters | A `FormatException` from `Load`; a `BODU-CAL-SYNTAX` diagnostic from `TryLoad`. | This page |
| A notable-date JSON document | `System.Text.Json`'s default depth limit of 64 | The same, from `LoadJson` and `TryLoadJson`. | This page |
| Imports | Resolved only through the resolver delegate you pass, which decides what a document can reach; a cycle is reported | A `BODU-CAL-IMPORT-CYCLE` diagnostic. | [Validation diagnostics](../../guides/calendar/validation-diagnostics.md) |
| A rule that fails validation | Every error is collected, with its `BODU-CAL-*` code | `NotableDateValidationException` from `Load`; `false` and the diagnostics from `TryLoad` and `TryLoadJson`. | [Validation diagnostics](../../guides/calendar/validation-diagnostics.md) |
| A compiled `.bcal` pack | The structure, every value and reference, and a SHA-256 digest of the payload are checked | `NotableDateBinaryFormatException`. | [Binary rule packs](../../guides/calendar/binary-rule-packs.md) |
| A plugin assembly | The trust policy runs before any plugin type is activated, and registration is atomic | `PluginNotTrustedException` and the other plugin exceptions. | [Plugin trust](../../guides/calendar/plugin-trust.md) |

Two of these are integrity checks, not trust boundaries. A pack's digest is unkeyed, so it detects corruption, but someone who edits a pack can recompute it, and a pack is not revalidated as a document is: load packs only from your own build or deployment, and load a document you do not trust with `TryLoad`. An admitted plugin runs with the full trust of your process; pin the exact bytes you reviewed with `FileHashPluginTrustPolicy`, as the [plugin trust guide](../../guides/calendar/plugin-trust.md) explains.

## Recurrence rules

`RecurrenceRule`, `CronExpression`, `RecurrenceSet`, and `AnchoredInterval` each have a `TryParse(s, out result, out failureMessage)` overload that names the defect instead of throwing, for rules a user types. A `CronExpression` search scans at most 400 years, one whole Gregorian cycle, so an expression that can never fire returns `null` rather than searching without end.

## Password hashes

`Argon2.Verify` and `Scrypt.Verify` read their cost parameters from the encoded hash, so a hash from an untrusted store can ask for any amount of work. Both cap the memory a verification may allocate at 2 GiB (an Argon2 `MemoryKiB` of at most 2^21, a scrypt `128 * N * r` of at most 2 GiB) and throw `ArgumentOutOfRangeException` for a hash that asks for more. A single 2 GiB verification is still expensive, so if the hashes can come from somewhere you do not control, check their parameters against the ones you issue before verifying. See [Argon2](../../guides/cryptography/argon2.md) and [scrypt](../../guides/cryptography/scrypt.md).

## Exchange-rate feeds

A web provider parses whatever its endpoint returns. A payload it cannot parse throws `ExchangeRateFormatException`, and a provider that builds its own `HttpClient` caps the response at `WebRateProviderOptions.MaxResponseContentBufferSize`, 64 MiB by default. The cap is not applied to a client that is passed in, and the dependency-injection registration passes one in from `IHttpClientFactory`, so under that registration set the named client's `MaxResponseContentBufferSize` yourself, as the [example host](hosting.md#one-host) does. See [resilience](../exchange-rates/concepts.md#resilience).

## Recommended settings for hostile input

For a service that reads files from people it does not trust:

| Input | Settings |
|---|---|
| Any | Reject an input over your size limit before opening it, and catch the reader's format exception as a rejection. |
| Compound files (`.doc`, `.xls`, `.msg`) | `CompoundReadStrategy.Streaming` or `Auto`, so a large file is not read into memory whole; `Strict` when conformance matters more than reading what other tools accept, `Compatible` otherwise; never `Minimal`. |
| `.pst` stores | `MaxNodeDataLength` lowered to the largest property or attachment you will read whole, streaming larger attachments with `OpenContentStream`; `Compatible`, or `Strict` to refuse tampered stores; `OmitUnresolvableValues` off. |
| Outlook messages | `MaxEmbeddedMessageDepth` and `MaxDecompressedRtfBytes` lowered to what you need, and `DecompressRtf` off if you do not read RTF bodies. |
| TOML, YAML, Bencode | The default `MaxDepth`, or lower; it cannot be raised past 64. |
| Calendar rules | `TryLoad` / `TryLoadJson` with a resolver that serves only your own resources; packs and plugins only from your own deployment. |
| Filters and globs supplied by users | The default `RegexMatchTimeout`. |

For example, opening an uploaded `.msg` with tightened limits:

```csharp
using Bodu.Formats.Outlook;
using Bodu.IO.Compound;

var options = new OutlookMessageReaderOptions
{
    ValidationLevel = CompoundValidationLevel.Strict,
    ReadStrategy = CompoundReadStrategy.Streaming,
    MaxEmbeddedMessageDepth = 4,
    MaxDecompressedRtfBytes = 8 * 1024 * 1024,
};

using FileStream source = File.OpenRead("upload.msg");
if (source.Length > 50 * 1024 * 1024)
{
    Console.WriteLine("Rejected: the file is larger than 50 MiB.");
    return;
}

try
{
    using var message = OutlookMessage.Open(source, options);
    Console.WriteLine(message.Subject);
}
catch (OutlookFormatException)
{
    Console.WriteLine("Rejected: the file is not a readable Outlook message.");
}
```

## Where to go next

- **[Errors and exceptions](errors-and-exceptions.md)** - every exception the limits above throw, and what it carries.
- **[Conformance and test vectors](conformance.md)** - the published vectors and corpora each reader is tested against.
- **[Outlook reader options and limits](../../guides/outlook/reader-options-and-limits.md)** - every Outlook limit, and what `Strict` changes.
- **[Calendar plugin trust](../../guides/calendar/plugin-trust.md)** - what the plugin trust gate does and does not guarantee.
