# IO.Pst Samples

A console application demonstrating the `Bodu.IO.Pst` container and the
`Bodu.Formats.Outlook.Pst` mail-store reader built on it. Run it with:

```bash
dotnet run --project samples/IO.Pst/Bodu.IO.Pst.Samples.PstBasics
```

The sample is offline and deterministic: PST files cannot be authored by the
library, so it ships a committed real fixture (`Data/sample1.pst`, 265 KB — a
Microsoft pstsdk test-corpus file, Apache-2.0; see `Data/NOTICE.md`).

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.IO.Pst.Samples.PstBasics` | `IsPstFile` detection and the open handshake (`Format` / `CryptMethod`, node census); the raw LTP views (the store node's property context, the root hierarchy table whose row identifiers are child-folder NIDs); `DataLength` pricing plus `OpenDataStream` chunked reads under `Strict` validation and the `BlockCacheSize` knob, with a truncated copy rejected inside the `PstFileException` family; and the same file as an `OutlookMailStore` — folders, messages, recipients, attachments, bodies, and named-property resolution | `Bodu.IO.Pst`, `Bodu.Formats.Outlook.Pst` |
