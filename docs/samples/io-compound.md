---
title: Runnable samples
---

# Runnable samples

The repository ships a runnable, self-contained sample project for `Bodu.IO.Compound` under
[`samples/IO.Compound/`](https://github.com/bslater/bodu/tree/master/samples/IO.Compound).
It is **offline and deterministic** — in-memory containers plus two small committed fixtures
(`golden-v3.cfb`, 8 KB; `sample1.doc`, 29 KB) — and is a member of `bodu.slnx`, built and
executed by CI. The README documents every scenario individually: its intent, what the code
does, the output to expect, and the APIs demonstrated.

Run it from the repository root:

```bash
dotnet run --project samples/IO.Compound/Bodu.IO.Compound.Samples.CompoundBasics
```

## The sample

### Bodu.IO.Compound.Samples.CompoundBasics

The OLE2 / Compound File Binary container — the structured-storage "filesystem in a file"
inside legacy Office documents — end to end:

- **AuthorAndReadBack** — bottom-up authoring with
  <xref:Bodu.IO.Compound.Builders.CompoundStorageBuilder> (nested storages, byte streams),
  `WriteTo` a `MemoryStream`, reopen with <xref:Bodu.IO.Compound.CompoundFile> and walk
  entries and stream contents back byte-for-byte — no file on disk.
- **OlePropertySets** — document metadata both ways: author a
  `\x05SummaryInformation` stream with the typed
  <xref:Bodu.IO.Compound.PropertySets.SummaryInformationBuilder>, read it back through
  `TryGetSummaryInformation`, then apply the same accessor to the committed Word fixture
  and print the title/author/application Word 2000 wrote into it.
- **DetectAndVersion** — `IsCompoundFile` signature probing (a `.doc` *is* an OLE2
  container; plain text is not), and the
  <xref:Bodu.IO.Compound.Builders.CompoundBuildOptions.Version> knob: the same tree authored
  as V3 (512-byte sectors, 2,560 bytes) vs V4 (4096-byte sectors, 20,480 bytes).
- **StreamsAndEntries** — walk the real `.doc`'s storage tree via
  <xref:Bodu.IO.Compound.CompoundStorage> and <xref:Bodu.IO.Compound.CompoundEntryInfo>
  (control-char stream names rendered printably) and read the `WordDocument` stream's head
  bytes — the envelope only, no Word knowledge.

*Package: `Bodu.IO.Compound`.*

## Guarded documentation

The guides under [`docs/guides/io-compound/`](../guides/io-compound/index.md) carry
compile-guarded snippets: examples marked with a `<!-- compile -->` sentinel are compiled
against the current public API by `DocumentationSnippetCompileTests` in the library's test
project (Regression tier).

## Related

- [IO.Compound guides](../guides/io-compound/index.md) — reading, authoring, property sets,
  and streaming/buffering.
- [Excel samples](excel.md) — the BIFF8 `.xls` reader built on this container format.
