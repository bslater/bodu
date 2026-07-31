---
title: Binary rule packs
---

# Binary rule packs

A binary rule pack (`.bcal`) is the compiled form of a notable-date document: a compact, integrity-checked, sealed encoding of a **validated** <xref:Bodu.Globalization.Calendar.NotableDateResource>. Packs are written at build/authoring time and loaded at run time without parsing or semantic validation — the trim- and AOT-friendly load path for calendar data.

## When to use a pack

- **Trimmed / native-AOT deployments.** Loading a pack touches no XML/JSON machinery and no reflection; every strategy is reconstructed through a closed table of one-byte discriminators. This is the data-driven alternative the plugin loader's `[RequiresUnreferencedCode]` annotations point at.
- **Startup cost.** A pack skips the parse → import-resolution → validation pipeline entirely; only bounds checks and an integrity digest run at load.
- **Sealed deployment artifacts.** A pack cannot carry anything the shipped engine does not already implement, and any corruption or tampering fails the payload digest.

## Producing and loading packs

<!-- compile -->
```csharp
// Compile: author (or load) a document, then save it as a pack. Build() runs first,
// so only content that passed the canonical loader's validation is ever encoded.
NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("corp.holidays")
    .AddNotableDate("company-day", "Company Day", NotableDateCategory.Observance, d => d
        .AddRule("default", r => r.Fixed(3, 14)));

builder.Save("corp-holidays.bcal");             // extension selects the binary format
builder.SaveBinary("corp-holidays.bcal");       // or explicitly

// Load: the runtime-side entry point skips parsing and validation.
using FileStream stream = File.OpenRead("corp-holidays.bcal");
NotableDateResource resource = NotableDateResourceLoader.LoadBinary(stream);
INotableDateService service = new NotableDateService(resource);
```

The symmetric low-level surface is <xref:Bodu.Globalization.Calendar.NotableDateBinaryResource> — `Write(resource, stream)` / `Read(stream)` — usable with any already-built resource, including the bundled catalogues.

A pack is **compiled output, not an authoring source**: `NotableDateDocumentBuilder.Load` rejects `.bcal` paths with `NotSupportedException`. Keep the XML/JSON document as the editable source of truth and recompile.

## Guarantees

| Guarantee | Meaning |
|---|---|
| **Pre-validated content** | The writer only accepts a built `NotableDateResource`; `SaveBinary` runs `Build()` (the canonical loader) first, so an invalid document can never reach a pack. |
| **Byte stability** | The same resource always encodes to the same bytes — dictionary content is key-sorted and string interning follows deterministic traversal — so build systems can rely on pack outputs for up-to-date checks. |
| **Integrity** | The header carries a SHA-256 digest of the payload; any corruption or modification fails the load before content is interpreted. |
| **Sealed** | The reader rejects unknown format versions, unknown discriminators, undefined enum values, out-of-range string references, truncation at any byte, trailing bytes, and values outside a model constructor's domain — always as <xref:Bodu.Globalization.Calendar.NotableDateBinaryFormatException> (a `FormatException`), never as an unrelated failure. |
| **Behavioural fidelity** | A round-tripped resource resolves identically to the original; the test suite pins resolved-occurrence parity for every bundled catalogue and a synthetic document covering every strategy, recurrence, and duration type. |

## Format layout (version 1)

Multi-byte integers are little-endian. `varuint` is the 7-bit variable-length encoding; signed values use ZigZag over `varuint`. Strings are referenced by 1-based index into a deduplicating table (index `0` = null); dates are day numbers.

```text
header   := "BCAL" version:u16 flags:u16 payload-sha256:32B
payload  := string-table body
string-table := count:varuint { length:varuint utf8-bytes }*
body     := resourceId schemaVersion resolution-policy
            adjustment-policies:count-prefixed
            notable-dates:count-prefixed
```

Rules encode nullable fields as presence bytes and their occurrence source as a marker byte (`1` = calculation strategy, `2` = recurrence) followed by a discriminator byte and that type's fields. The discriminator tables enumerate the engine's 13 calculation strategies, 4 recurrence strategies, and 2 duration definitions exhaustively; additions require a new format version, which this reader rejects.

## Where to go next

- [Builder round-trip guarantees](round-trip-guarantees.md) — the XML/JSON serialization contract the pack compiler builds on.
- [Authoring with the notable-date builder](notable-date-builder.md) — producing the documents packs are compiled from.
- [Calendar plugin trust](plugin-trust.md) — why data packs are the AOT-compatible alternative to code plugins.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
