# Bodu.IO.Compound.Samples.CompoundBasics

The OLE2 / Compound File Binary (CFB) container via `Bodu.IO.Compound` — the
structured-storage envelope inside legacy Office files (`.doc`, `.xls`, `.msg`): a
"filesystem in a file" of nested storages and named streams. Four scenarios cover authoring
with the staged builder API and reading back, the OLE property sets that carry document
metadata, format detection plus the v3/v4 version knob, and walking a real committed `.doc`.
Everything runs offline against in-memory containers and two committed fixtures
(`Data/golden-v3.cfb`, 8 KB; `Data/sample1.doc`, 29 KB — copied from the library's test
fixtures).

```bash
dotnet run --project samples/IO.Compound/Bodu.IO.Compound.Samples.CompoundBasics
```

## Scenario 1 — AuthorAndReadBack

**Intent.** Show the authoring loop: containers are built bottom-up with
`CompoundStorageBuilder` (storages nest, streams carry bytes), written to any `Stream`, and
read back through `CompoundFile` — a full structured-storage round trip with no file on disk
and no application-format knowledge.

**What it does.** Creates a root with a `Manifest` stream and a nested `Payload` storage
holding two more streams, writes the container to a `MemoryStream`, reopens it, enumerates
the root and nested entries with their types and sizes, and reads a stream's text content
back verbatim.

**What to expect.**

```text
authored container: 3072 bytes
  /Payload (Storage, 0 bytes)
  /Manifest (Stream, 20 bytes)
  /Payload/ReadMe (45 bytes)
  /Payload/Numbers (5 bytes)
  /Payload/ReadMe content: 'Structured storage is a filesystem in a file.'
```

**APIs demonstrated.** `CompoundStorageBuilder.CreateRoot` / `.AddStorage` / `.AddStream`,
`CompoundStorageBuilder.WriteTo(Stream)`, `CompoundFile.Open`, `CompoundStorage.EnumerateEntries`
/ `.EnumerateStreams` / `.OpenStorage` / `.OpenStream`.

## Scenario 2 — OlePropertySets

**Intent.** Show OLE property sets — the metadata (title, author, timestamps) legacy Office
files carry in the well-known `\x05SummaryInformation` stream — in both directions: author
one with the typed builder, and read one back from a real Word file with the typed accessor,
no raw property-set parsing required.

**What it does.** Fills a `SummaryInformationBuilder` (title, author, application, creation
time), embeds it on a new writable container through `CompoundFile.SetSummaryInformation`,
stamps the root storage's `ClassId` (the OLE2 file-type discriminator), persists it with the
asynchronous `CommitAsync`, reopens it and reads the values and class id back through
`TryGetSummaryInformation` and `RootStorage.ClassId`; then applies the same accessor to the
committed `sample1.doc` and prints the metadata Word 2000 wrote into it decades ago.

**What to expect.**

```text
authored : 'Quarterly figures' by Bodu Sample (created 2026-07-01)
authored : root class id 00020906-0000-0000-c000-000000000046
sample1.doc: title='Sample document created with MS Word', author='steve', app='Microsoft Word 9.0'
```

**APIs demonstrated.** `SummaryInformationBuilder.ToPropertySet`, `CompoundFile.Create`,
`CompoundFile.SetSummaryInformation`, the settable `CompoundStorage.ClassId`,
`CompoundFile.CommitAsync`, `CompoundFile.TryGetSummaryInformation`, the typed
`SummaryInformation` properties.

## Scenario 3 — DetectAndVersion

**Intent.** Two operational questions: "is this byte buffer an OLE2 container at all?"
(answered from the 8-byte signature by `IsCompoundFile`, no parse required — the right
pre-check before handing bytes to a full open), and "which format version should I author?"
(`CompoundBuildOptions.Version`: v3 with 512-byte sectors vs v4 with 4096-byte sectors — a
size/alignment trade-off that is directly visible in the emitted bytes).

**What it does.** Probes the two committed fixtures and a plain-text buffer with
`IsCompoundFile` (a `.doc` *is* an OLE2 container), then authors the same one-stream tree as
V3 and as V4 and prints the dramatic size difference, reopening the V4 container to show
both read through the same API.

**What to expect.**

```text
golden-v3.cfb : IsCompoundFile = True
sample1.doc   : IsCompoundFile = True (a .doc IS an OLE2 container)
plain text    : IsCompoundFile = False
same content authored as V3:   2560 bytes (512-byte sectors)
same content authored as V4:  20480 bytes (4096-byte sectors)
```

**APIs demonstrated.** `CompoundFile.IsCompoundFile(ReadOnlySpan<byte>)`,
`CompoundBuildOptions.Version` + `CompoundFileVersion.V3`/`.V4`.

## Scenario 4 — StreamsAndEntries

**Intent.** Read a real-world container without any application knowledge: the committed
Word 97-2003 fixture is walked purely through the storage/stream surface, showing the
directory metadata `CompoundEntryInfo` exposes and that stream bytes are just bytes — the
package's whole job is the envelope, not the Word format.

**What it does.** Recursively dumps `sample1.doc`'s tree (streams with sizes, the
`ObjectPool` storage), rendering the control characters that prefix well-known stream names
(`\x05SummaryInformation`, `\x01CompObj`) printably, then opens the `WordDocument` stream
and prints its first eight bytes.

**What to expect.**

```text
  1Table (Stream, 8375 bytes)
  \x01CompObj (Stream, 106 bytes)
  ObjectPool (Storage)
  WordDocument (Stream, 9280 bytes)
  \x05SummaryInformation (Stream, 4096 bytes)
  \x05DocumentSummaryInformation (Stream, 4096 bytes)
'WordDocument' head bytes: ECA5C10037200904 (9280 bytes total)
```

**APIs demonstrated.** `CompoundFile.OpenRead(path)`, recursive
`EnumerateEntries`/`EnumerateStorages`, `CompoundEntryInfo.Name`/`.EntryType`/`.Length`,
`CompoundStorage.TryOpenStream` + raw stream reads.

## Layout

```text
Bodu.IO.Compound.Samples.CompoundBasics/
  Program.cs                        # runs the scenarios in order
  Data/golden-v3.cfb                # committed 8 KB v3 container fixture
  Data/sample1.doc                  # committed 29 KB Word 97-2003 fixture
  Scenarios/AuthorAndReadBack.cs
  Scenarios/OlePropertySets.cs
  Scenarios/DetectAndVersion.cs
  Scenarios/StreamsAndEntries.cs
```

> Note: the scenario class is named `OlePropertySets` (not `PropertySets`) because inside
> the `Bodu.IO.Compound.*` namespace tree the simple name `PropertySets` resolves to the
> `Bodu.IO.Compound.PropertySets` namespace, not a type — the same shadowing rule the
> Text.Formats samples document.

## Related

- `Bodu.Formats.Excel.Binary` samples (`samples/Formats.Excel/`) — the BIFF8 `.xls` reader
  built on this container format.
- Guides: `docs/guides/io-compound/`.
