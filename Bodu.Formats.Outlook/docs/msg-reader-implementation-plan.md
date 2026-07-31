# Bodu.Formats.Outlook — `.msg` reader kickoff and implementation plan

**Date:** 2026-07-31
**Status:** **Executed — M0–M4 delivered 2026-07-31.** Section 1
executed tranche **T5** of the `Bodu.IO.Compound` plan (the MS-OXMSG
substrate-readiness review); the implementation then landed per the
tranche plan below. Deliberate deviations from the sketches, each
recorded at its landing commit: *(1)* the shared model's `MapiProperty`
/ `MapiPropertyCollection` gained public constructors so the format
packages (and the future `.pst` reader) can construct them without
cross-package `InternalsVisibleTo`; *(2)* named-property resolution
ships as `TryGetNamedPropertyId(name, out ushort id)` +
`TryGetPropertyName(tag, out name)` rather than the sketched
`TryGetNamedPropertyTag` — the MS-OXMSG mapping is identifier-level,
not type-level, so returning a typed tag would have invented a type;
*(3)* scalar conveniences landed in M2 with the property surface (they
are one-line accessors), leaving M4 the body trio + `CompressedRtf`;
*(4)* Regression runs entirely on synthetic fixtures authored through
`Bodu.IO.Compound` — the real-world corpus with a provenance
`NOTICE.md` is a recorded roadmap follow-up.
**Relates to:** [`ROADMAP.md`](../../ROADMAP.md) — *New library
candidates → `Bodu.Formats.Outlook.Msg`*;
[`Bodu.IO.Compound/docs/roadmap-implementation-plan.md`](../../Bodu.IO.Compound/docs/roadmap-implementation-plan.md)
— §8 (T5).

This plan turns the roadmap's `.msg` candidate into sequenced, scoped
work. The `.msg` (Outlook message) container *is* an OLE2 / Compound
File Binary container, so the reader inherits the entire CFB read stack
from `Bodu.IO.Compound` and adds only the MS-OXMSG layer: the property
stream, the `__substg1.0_…` value streams, named-property resolution,
and the recipient / attachment storage conventions. Per the roadmap,
the namespace flattens to **`Bodu.Formats.Outlook`** (the
`Bodu.Formats.Excel` convention) so the MAPI property surface is shared
with the future `.pst` reader (see the companion exploration,
[`Bodu.IO.Pst/docs/pst-container-exploration.md`](../../Bodu.IO.Pst/docs/pst-container-exploration.md))
rather than owned by either format package.

---

## 0. Ground rules

Every tranche inherits the repository conventions
([`CLAUDE.md`](../../CLAUDE.md)); the ones that recur in this plan:

- **Tier.** Both new packages start **Preview** (README tier badge) and
  are promoted to Stable once the surface has settled against a real
  corpus — the same path the exchange-rate providers follow.
- **Validation and messages.** Public surfaces validate through
  `ThrowHelper.ThrowIf…` (Core) or a domain throw helper; all
  user-facing text lands in a `<Domain>ResourceStrings.resx` with the
  established key prefixes — never string literals.
- **Tests.** MSTest partials with a member-named backbone; KAT rows via
  the `Bodu.Test.Kat` generics wired through `KatDisplayName`; new
  tests default to BVT, exhaustive sweeps are
  `[TestCategory("Regression")]`, one happy-path Smoke test per primary
  type.
- **Docs.** Every public member owes full XML docs (CS1591 breaks the
  build). A DocFX guide section and a runnable sample land with the
  first shipped surface (M4), snippet-compile guarded like the
  `Bodu.IO.Compound` guides.
- **Commit discipline.** One branch per session; each lettered item
  lands as an ordered commit sequence — production code, test backbone,
  Regression sweeps, guide/sample updates — never one monolithic
  commit. Each item's final commit leaves
  `dotnet test bodu.slnx --settings bvt.runsettings` green.

## 1. T5 — substrate-readiness review (MS-OXMSG vs `Bodu.IO.Compound`)

Executed 2026-07-31 against the current tree. The review walks every
container-level structure MS-OXMSG requires against the shipped
`CompoundFile` / `CompoundStorage` / `CompoundStream` surface.

**Verdict: the substrate is ready — no new `Bodu.IO.Compound` API is
required.** Every `.msg` structural need maps onto an existing member:

| MS-OXMSG requirement | Container surface | Status |
| --- | --- | --- |
| Open the container from path or stream, read-only, buffered or streaming | `CompoundFile.OpenRead(path)` / `Open(stream, options)` with `CompoundReadStrategy` | ✓ |
| Sniff "is this a CFB file at all" before parsing | `CompoundFile.IsCompoundFile(Stream / ReadOnlySpan<byte>)` | ✓ |
| Root-CLSID discrimination (`.msg` roots conventionally record `{00020D0B-0000-0000-C000-000000000046}`) | `CompoundFile.RootStorage.Stat.ClassId` (`CompoundEntryInfo.ClassId`) | ✓ |
| Exact-name stream lookup (`__properties_version1.0`, `__substg1.0_HHHHTTTT`) | `CompoundStorage.OpenStream` / `TryOpenStream` — keyed through `CompoundNameComparer`, the format's own case-insensitive, length-first relationship, so lookups match however the writer cased the name | ✓ |
| Enumerate child storages by prefix (`__recip_version1.0_#NNNNNNNN`, `__attach_version1.0_#NNNNNNNN`) | `CompoundStorage.EnumerateStorages()` / `EnumerateEntries()`; the `#NNNNNNNN` suffix is plain-string filtering at the format layer | ✓ |
| Storage recursion for nested attached messages (`__substg1.0_3701000D` storage inside an attachment storage) | `CompoundStorage.OpenStorage` / `TryOpenStorage` — unbounded depth | ✓ |
| Named-property mapping storage (`__nameid_version1.0`) | Plain child storage + stream reads; no special container support needed | ✓ |
| Many tiny streams (most `.msg` property streams sit in the mini stream) | Mini-FAT / mini-stream reads are first-class in the reader; `CompoundStream.AsMemory()` / `ReadAllBytes()` avoid per-read overhead for small payloads | ✓ |
| Tolerance for sloppy real-world writers | `CompoundValidationLevel` (Strict / Compatible / Minimal) on `CompoundFileOptions` | ✓ |
| Async reads for large attachment payloads | Streaming-mode `CompoundStream.ReadAsync` is truly asynchronous (Compound plan T4) | ✓ |

Confirmations that shaped the plan rather than the substrate:

- **`__properties_version1.0` is not MS-OLEPS.** It is `.msg`'s own
  fixed-record layout, so `OlePropertySet` / `PropertySetReader` are
  deliberately *not* reused — parsing belongs in this package (the
  Compound plan predicted exactly this split).
- **No convenience gap surfaced.** The one candidate ("enumerate
  streams whose name starts with a prefix") is a one-line LINQ filter
  over `EnumerateStreams()` and does not justify container API.
- **The write side is already covered** for fixture generation:
  `CompoundFile.Create` + `CompoundStorageBuilder` can author synthetic
  `.msg` fixtures byte-for-byte (see §6), which removes the need for
  any Office tooling in the test loop.

Nothing comes back to the `Bodu.IO.Compound` plan as an additive item.
T5 closes with this section.

## 2. Package and namespace shape

Two packages, one flattened namespace — the `Bodu.Financial.ExchangeRates`
convention the roadmap prescribes:

| Package | Folder | Namespace | Responsibility |
| --- | --- | --- | --- |
| `Bodu.Formats.Outlook` | `Bodu.Formats.Outlook/` | `Bodu.Formats.Outlook` | The shared MAPI value model: property tags / types / values, the property-collection surface, named-property identities, recipient / attachment value types and enums, and the shared exception hierarchy. **No container knowledge.** |
| `Bodu.Formats.Outlook.Msg` | `Bodu.Formats.Outlook.Msg/` | `Bodu.Formats.Outlook` (public) + `Bodu.Formats.Outlook.Msg` (internal record layer) | The `.msg` reader over `Bodu.IO.Compound`: the session type, the property-stream / substg decoding, named-property resolution, and body conveniences. |

The future `Bodu.Formats.Outlook.Pst` reader references the shared
package and `Bodu.IO.Pst`, never `.Msg` — so neither format package
owns the value model. The internal `.Msg` layer mirrors
`Bodu.Formats.Excel.Biff8` exactly: an internal namespace in a flat
`Formats.Outlook.Msg/` folder, granted to the test assembly via
`InternalsVisibleTo`.

### Settled decisions

| # | Decision | Position |
| --- | --- | --- |
| D1 | Package split | **Two packages from the start.** The shared model is small, but retro-splitting it out of a shipped `.Msg` package later would be a breaking move; the ExchangeRates precedent shows the split costs almost nothing up front. |
| D2 | Read/write scope | **Read-only.** `Bodu.IO.Compound` could author `.msg` containers, but MS-OXMSG writing (property-stream bookkeeping, named-property allocation) is its own project. Authoring is a demand-driven follow-on, not v1. |
| D3 | ANSI (`PT_STRING8`) decoding | **Supported, via `System.Text.Encoding.CodePages`.** The `.Msg` package registers the provider and resolves the code page from `PidTagMessageCodepage` / `PidTagInternetCodepage`, falling back to Windows-1252. Unicode (`PT_UNICODE`, UTF-16LE) needs no provider. The shared package stays dependency-free. |
| D4 | Property surface shape | **Raw tag-addressed collection + curated typed conveniences.** Every property is reachable through `MapiPropertyCollection`; `OutlookMessage` layers well-known conveniences (`Subject`, `BodyText`, sender, dates) on top. No MAPI session semantics, no `IMessage` emulation. |
| D5 | Named properties | **Resolved eagerly at open.** `__nameid_version1.0` is small; parsing it once gives bidirectional lookup (`MapiNamedProperty` ↔ tag ≥ `0x8000`) for the message and every nested message, which share the map per MS-OXMSG. |
| D6 | RTF body | **Ship the compressed-RTF decoder (MS-OXRTFCP).** `PidTagRtfCompressed` is the *only* body many real messages carry; without the decoder the reader cannot surface a body at all for them. The algorithm is small (dictionary-seeded LZ + CRC). RTF→HTML de-encapsulation (MS-OXRTFEX) stays out of scope. |
| D7 | Well-known property ids | **Curated constants, not the full catalogue.** `MapiPropertyIds` carries the ids the conveniences and tests need (a few dozen). The full ~2000-entry catalogue is a future tooling-generator candidate (the CRC-catalogue pattern), not hand-typed now. |

## 3. Public API sketch

### `Bodu.Formats.Outlook` (shared model)

```csharp
namespace Bodu.Formats.Outlook;

/// <summary>The 16-bit MAPI property type codes (PT_*).</summary>
public enum MapiPropertyType : ushort
{
    Unspecified = 0x0000, Null = 0x0001, Int16 = 0x0002, Int32 = 0x0003,
    Float = 0x0004, Double = 0x0005, Currency = 0x0006,
    AppTime = 0x0007, ErrorCode = 0x000A, Boolean = 0x000B,
    Object = 0x000D, Int64 = 0x0014, String8 = 0x001E,
    Unicode = 0x001F, SystemTime = 0x0040, Guid = 0x0048,
    Binary = 0x0102,
}

/// <summary>A 32-bit MAPI property tag: id (high 16) + type (low 16).</summary>
public readonly struct MapiPropertyTag : IEquatable<MapiPropertyTag>
{
    public MapiPropertyTag(ushort id, MapiPropertyType type);
    public ushort Id { get; }
    public MapiPropertyType Type { get; }
    public bool IsMultiValued { get; }          // 0x1000 flag
    public bool IsNamed { get; }                // Id >= 0x8000
    public uint Value { get; }                  // the raw 32-bit tag
    public override string ToString();          // "0x0037001F"
}

/// <summary>A named-property identity: a property-set GUID plus a
/// numeric id or a string name (MS-OXPROPS named properties).</summary>
public readonly struct MapiNamedProperty : IEquatable<MapiNamedProperty>
{
    public Guid PropertySetId { get; }
    public uint? Id { get; }
    public string? Name { get; }
}

/// <summary>A single decoded property: tag + CLR value.</summary>
public sealed class MapiProperty
{
    public MapiPropertyTag Tag { get; }
    public object? Value { get; }               // string, int, long, bool,
                                                // double, decimal, DateTimeOffset,
                                                // Guid, byte[], arrays thereof
}

/// <summary>Tag-addressed read-only property collection with typed
/// accessors, shared by messages, recipients, and attachments.</summary>
public sealed class MapiPropertyCollection : IReadOnlyCollection<MapiProperty>
{
    public bool Contains(MapiPropertyTag tag);
    public bool TryGetValue(MapiPropertyTag tag, out MapiProperty property);
    public string? GetString(ushort id);
    public int? GetInt32(ushort id);
    public bool? GetBoolean(ushort id);
    public DateTimeOffset? GetDateTime(ushort id);
    public ReadOnlyMemory<byte>? GetBinary(ushort id);
    // …Int64 / Double / Guid / string[] counterparts
}

/// <summary>Curated well-known property ids (PidTag*).</summary>
public static class MapiPropertyIds
{
    public const ushort Subject = 0x0037;
    public const ushort SenderName = 0x0C1A;
    public const ushort Body = 0x1000;
    public const ushort RtfCompressed = 0x1009;
    public const ushort Html = 0x1013;
    public const ushort MessageDeliveryTime = 0x0E06;
    public const ushort ClientSubmitTime = 0x0039;
    public const ushort TransportMessageHeaders = 0x007D;
    public const ushort MessageCodepage = 0x3FFD;
    public const ushort AttachFilename = 0x3704;
    public const ushort AttachLongFilename = 0x3707;
    public const ushort AttachMethod = 0x3705;
    public const ushort AttachDataBinary = 0x3701;
    public const ushort DisplayName = 0x3001;
    public const ushort EmailAddress = 0x3003;
    public const ushort RecipientType = 0x0C15;
    // … the rest of the curated set
}

public enum OutlookRecipientType { To = 1, Cc = 2, Bcc = 3 }

public enum OutlookAttachmentMethod
{
    None = 0, ByValue = 1, ByReference = 2, ByReferenceResolve = 3,
    ByReferenceOnly = 4, EmbeddedMessage = 5, Ole = 6,
}

/// <summary>Shared exception hierarchy.</summary>
public class OutlookFormatException : FormatException { … }
```

### `Bodu.Formats.Outlook.Msg` (reader; public types in the flattened namespace)

```csharp
namespace Bodu.Formats.Outlook;

/// <summary>A disposable, read-only session over a `.msg` file.
/// The root message and every nested attached message share the
/// session's container and named-property map.</summary>
public sealed class OutlookMessage : IDisposable
{
    public static OutlookMessage OpenRead(string path);
    public static OutlookMessage OpenRead(Stream stream, bool leaveOpen = false);
    public static OutlookMessage Open(Stream stream, OutlookMessageReaderOptions options,
        bool leaveOpen = false);
    public static bool IsMsgFile(Stream stream);   // CFB sniff + root-entry check

    // The raw surface — every decoded property of the message object:
    public MapiPropertyCollection Properties { get; }
    public IReadOnlyList<OutlookRecipient> Recipients { get; }
    public IReadOnlyList<OutlookAttachment> Attachments { get; }

    // Curated conveniences over well-known tags (all nullable):
    public string? Subject { get; }
    public string? SenderName { get; }
    public string? SenderEmailAddress { get; }
    public string? InternetMessageId { get; }
    public string? TransportMessageHeaders { get; }
    public DateTimeOffset? SentTime { get; }        // ClientSubmitTime
    public DateTimeOffset? ReceivedTime { get; }    // MessageDeliveryTime
    public string? BodyText { get; }                // PidTagBody
    public string? BodyHtml { get; }                // PidTagHtml (bytes → codepage)
    public string? BodyRtf { get; }                 // RtfCompressed, decompressed (D6)

    // Named-property resolution (bidirectional; shared by nested messages):
    public bool TryGetNamedPropertyTag(MapiNamedProperty name, out MapiPropertyTag tag);
    public bool TryGetPropertyName(MapiPropertyTag tag, out MapiNamedProperty name);
}

public sealed class OutlookRecipient
{
    public OutlookRecipientType? RecipientType { get; }
    public string? DisplayName { get; }
    public string? EmailAddress { get; }
    public string? AddressType { get; }             // "SMTP", "EX", …
    public MapiPropertyCollection Properties { get; }
}

public sealed class OutlookAttachment
{
    public OutlookAttachmentMethod Method { get; }
    public string? FileName { get; }                // long name preferred
    public string? ContentId { get; }
    public long? Size { get; }
    public MapiPropertyCollection Properties { get; }

    public Stream OpenContentStream();              // ByValue payload
    public OutlookMessage OpenMessage();            // EmbeddedMessage payload
}

public sealed class OutlookMessageReaderOptions
{
    public CompoundValidationLevel ValidationLevel { get; init; }
    public CompoundReadStrategy ReadStrategy { get; init; }
    public bool DecompressRtf { get; init; } = true;
}

/// <summary>Msg-specific failures, deriving from the shared base.</summary>
public sealed class OutlookMsgFormatException : OutlookFormatException { … }
```

Design notes:

- `OutlookMessage` is both the session *and* the document — a `.msg`
  file *is* one message, so no `ExcelBinaryWorkbook`-style separate
  session type is warranted. A nested `OutlookMessage` from
  `OpenMessage()` shares (never owns) the parent's container; disposing
  the root disposes everything.
- All conveniences are lazy over `Properties` — no double decode.
- `OpenContentStream()` hands back the underlying `CompoundStream`
  (read-only cursor), so large attachments stream without
  materialization; `ReadAsync` is truly async under the streaming
  strategy per T5.

## 4. Internal structure (the `Bodu.Formats.Outlook.Msg` record layer)

Mirrors `Bodu.Formats.Excel.Biff8` — internal, flat folder
`Formats.Outlook.Msg/`, exercised directly by tests via
`InternalsVisibleTo`:

| Type | Responsibility |
| --- | --- |
| `MsgStreamNames` | Name constants and formatters: `__properties_version1.0`, `__nameid_version1.0`, `__substg1.0_{id:X4}{type:X4}`, `__recip_version1.0_#{n:X8}`, `__attach_version1.0_#{n:X8}`, the `-{n:X8}` multi-value suffix. |
| `MsgPropertyStreamReader` | Parses `__properties_version1.0`: the three header shapes (root: 32 bytes with next-recipient/attachment ids and counts; embedded message: 24 bytes; recipient / attachment: 8 bytes), then the 16-byte fixed entries (tag, flags, 8-byte value-or-size). |
| `MsgPropertyDecoder` | Materializes `MapiProperty` values: fixed-length types inline from the entry; variable-length types from the matching `__substg1.0_` stream (trusting the stream length over the recorded size, which includes the string terminator); multi-valued types via the length stream + per-element streams. |
| `MsgNamedPropertyMap` | Parses `__nameid_version1.0` (GUID stream `00020102`, entry stream `00030102`, string stream `00040102`) into the bidirectional map; the per-bucket hash streams are ignored on read. |
| `MsgStorageWalker` | Enumerates and orders the `#NNNNNNNN`-suffixed recipient / attachment storages, cross-checking counts against the property-stream header under Strict validation. |
| `CompressedRtf` | The MS-OXRTFCP decoder (COMPRESSED / UNCOMPRESSED magics, dictionary-seeded LZ, CRC check). |

## 5. Project scaffolding

- Both csprojs follow `Bodu.Formats.Excel.Binary.csproj`: `net8.0`,
  `<RootNamespace>Bodu</RootNamespace>`, `InternalsVisibleTo` for the
  test assembly, resx + Designer wiring
  (`OutlookResourceStrings.resx` in the shared package,
  `OutlookMsgResourceStrings.resx` in the reader).
- References: shared package → `Bodu.Core` only; `.Msg` → shared +
  `Bodu.IO.Compound` + `System.Text.Encoding.CodePages` (D3).
- Solution: four project entries in `bodu.slnx` (two `src`, two
  `test`) in their own folders, matching the existing layout.
- READMEs with the **Preview** tier badge; `CLAUDE.md` project-table
  rows and `ROADMAP.md` per-project sections land with M0.

## 6. Fixtures and tests

**Synthetic fixtures are authored with `Bodu.IO.Compound` itself.**
`CompoundFile.Create` + `CompoundStorageBuilder` can emit any `.msg`
shape the tests need — minimal message, Unicode vs ANSI strings,
multi-valued properties, recipient/attachment fan-out, nested
attached messages, deliberately malformed property streams — with
byte-exact control and zero Office tooling. A small internal
`MsgFixtureBuilder` in the test project wraps the patterns.

**A real-world reference corpus** (a handful of `.msg` files exported
from real mail clients, provenance recorded in a `NOTICE.md` exactly
like `Bodu.IO.Compound/test/Fixtures/Reference/`) anchors Regression:
every fixture must open, decode all properties, and surface non-null
conveniences where the manifest says so. Corpus candidates: the
permissively licensed test files of existing open-source readers, plus
self-authored exports; each file's licence is verified before check-in.

Test layout (per the conventions):

```text
OutlookMessageTests.cs                    // shared POCOs / suppliers
OutlookMessageTests.OpenRead.cs
OutlookMessageTests.Properties.cs
OutlookMessageTests.Recipients.cs
OutlookMessageTests.Attachments.cs
OutlookMessageTests.Bodies.cs             // subject partial: Text/Html/Rtf
OutlookMessageTests.NamedProperties.cs
MapiPropertyTagTests.cs / .Equality.cs
MapiPropertyCollectionTests.TryGetValue.cs / .GetString.cs / …
MsgPropertyStreamReaderTests.cs           // internal layer, per member
CompressedRtfTests.Decompress.cs          // MS-OXRTFCP §3 published vectors
```

KAT usage: `ValidKat<byte[], string>` for compressed-RTF vectors (the
spec publishes known answers), `InvalidKat<byte[]>` for malformed
property-stream sweeps, `RoundTripKat<,>` where fixture authoring +
re-read applies. Smoke: one `OpenRead` happy path on the minimal
synthetic message.

## 7. Tranches

| Tranche | Item | Depends on | Notes |
| --- | --- | --- | --- |
| **M0** | Scaffolding: both projects + tests in `bodu.slnx`, resx, READMEs, roadmap/CLAUDE rows | — | Build stays green with empty surfaces. |
| **M1** | Shared MAPI model (`Bodu.Formats.Outlook`): tags, types, values, collection, named identities, enums, exceptions | M0 | Pure value types; fully unit-testable without fixtures. The `.pst` gate: `Bodu.IO.Pst` work can start once M1 ships. |
| **M2** | Core `.msg` decode: `OutlookMessage.OpenRead`, property stream, substg values (incl. multi-valued), ANSI/Unicode strings | M1 | The heart of the package; synthetic fixtures land here. |
| **M3** | Recipients, attachments, nested messages, named-property resolution | M2 | Completes the structural surface. |
| **M4** | Body conveniences + `CompressedRtf`; DocFX guide + runnable sample; reference corpus Regression sweep | M2 (RTF), M3 (corpus) | Ships the Preview package. |

Each tranche is a separate commit sequence; M2 and the RTF half of M4
are independent after M1 and can interleave if a session allows.

## 8. Out of scope

- **Writing `.msg` files** — demand-driven follow-on (D2).
- **MAPI session semantics** (`IMessage`/`IMAPIProp` emulation,
  property flags enforcement, store behaviour) — this is a file
  reader, not a MAPI implementation.
- **RTF→HTML/Text de-encapsulation** (MS-OXRTFEX) — consumers get the
  decompressed RTF verbatim.
- **TNEF (`winmail.dat`, MS-OXTNEF)** — a separate candidate; would
  reuse the shared model if pursued.
- **S/MIME decryption / signed-content unwrapping** — cryptographic
  message processing stays out of a format reader.
- **OLE attachment rendering** (`AttachMethod = Ole`) — the raw
  storage is reachable through `Properties`; interpreting it is not.
