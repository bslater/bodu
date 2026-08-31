# Bodu.IO.Pst.Samples.PstBasics

A console walk-through of the PST stack, from the raw container to the mail-store view:

| Scenario | Shows |
|---|---|
| `DetectAndOpen` | `PstFile.IsPstFile` probing, the open handshake (`Format` / `CryptMethod`), and a node census by `PstNodeType`. |
| `NodesAndProperties` | The two LTP views without MAPI semantics: the message-store node's property context, and the root folder's hierarchy table whose row identifiers are child-folder NIDs. |
| `StreamingAndValidation` | `DataLength` pricing, `OpenDataStream` chunked reads under `Strict` validation, the `BlockCacheSize` knob, and a truncated copy rejected inside the `PstFileException` family. |
| `ReadMailStore` | The same file through `Bodu.Formats.Outlook.Pst`: `OutlookMailStore` folders, messages, recipients, attachments, bodies, and named-property resolution. |

Run it from the repository root:

```bash
dotnet run --project samples/IO.Pst/Bodu.IO.Pst.Samples.PstBasics
```

Everything runs offline against the committed `Data/sample1.pst` (a Microsoft
pstsdk test-corpus file, Apache-2.0 — see `Data/NOTICE.md` for provenance).
PST files cannot be authored by this library, so the sample ships a real
fixture rather than generating one; point `Program.SamplePath` at any other
Unicode `.pst` to explore your own archive.
