# IO.Pst Samples

A console application demonstrating the `Bodu.IO.Pst` container and the
`Bodu.Formats.Outlook.Pst` mail-store reader built on it. Run it with:

```bash
dotnet run --project samples/IO.Pst/Bodu.IO.Pst.Samples.PstBasics
```

The sample is offline and deterministic: PST files cannot be authored by the
library, so it ships two committed real fixtures — `Data/sample1.pst` (Unicode
format) and `Data/sample2.pst` (ANSI format), 265 KB each, Microsoft pstsdk
test-corpus files under Apache-2.0; see `Data/NOTICE.md`.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.IO.Pst.Samples.PstBasics` | `IsPstFile` detection and the open handshake (`Format` / `CryptMethod`, node census) over a Unicode and an ANSI file side by side; the raw LTP views (the store node's property context, the root hierarchy table whose row identifiers are child-folder NIDs); `DataLength` pricing plus `OpenDataStream` chunked reads under `Strict` validation and the `BlockCacheSize` knob, with a truncated copy rejected inside the `PstFileException` family; and both files as an `OutlookMailStore` — folders, messages, recipients, attachments, bodies, and named-property resolution, with the ANSI store's code-page strings decoded through the same accessors | `Bodu.IO.Pst`, `Bodu.Formats.Outlook.Pst` |
