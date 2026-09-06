# Bodu.IO.Pst.Samples.PstBasics

A console walk-through of the PST stack, from the raw container to the mail-store view:

| Scenario | Shows |
|---|---|
| `DetectAndOpen` | `PstFile.IsPstFile` probing, the open handshake (`Format` / `CryptMethod`), and a node census by `PstNodeType` — over the Unicode sample and the ANSI sample side by side, showing the one surface serving both formats. |
| `NodesAndProperties` | The two LTP views without MAPI semantics: the message-store node's property context, and the root folder's hierarchy table whose row identifiers are child-folder NIDs. |
| `StreamingAndValidation` | `DataLength` pricing, `OpenDataStream` chunked reads under `Strict` validation, the `BlockCacheSize` knob, and a truncated copy rejected inside the `PstFileException` family. |
| `ReadMailStore` | The same files through `Bodu.Formats.Outlook.Pst`: `OutlookMailStore` folders, messages, recipients, attachments, bodies, and named-property resolution, then the ANSI store's code-page strings decoded through the same accessors. |

Run it from the repository root:

```bash
dotnet run --project samples/IO.Pst/Bodu.IO.Pst.Samples.PstBasics
```

Everything runs offline against the committed `Data/sample1.pst` (Unicode
format) and `Data/sample2.pst` (ANSI format), both Microsoft pstsdk test-corpus
files under Apache-2.0 — see `Data/NOTICE.md` for provenance. PST files cannot
be authored by this library, so the sample ships real fixtures rather than
generating them; point `Program.SamplePath` or `Program.AnsiSamplePath` at any
other `.pst` of either format to explore your own archive.
