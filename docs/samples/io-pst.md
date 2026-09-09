---
title: Runnable samples
---

# Runnable samples

The repository ships a runnable, self-contained sample project for `Bodu.IO.Pst` and the
`Bodu.Formats.Outlook.Pst` mail-store reader built on it under
[`samples/IO.Pst/`](https://github.com/bslater/bodu/tree/master/samples/IO.Pst).
It is **offline and deterministic**: PST files cannot be authored by the library, so the sample
ships two committed real fixtures — `Data/sample1.pst` (Unicode format) and `Data/sample2.pst`
(ANSI format), 265 KB each, Microsoft pstsdk test-corpus files under Apache-2.0 (see
`Data/NOTICE.md` for provenance). It is a member of `bodu.slnx`, built and executed by CI. The
README documents every scenario individually: its intent, what the code does, the output to
expect, and the APIs demonstrated.

Run it from the repository root:

```bash
dotnet run --project samples/IO.Pst/Bodu.IO.Pst.Samples.PstBasics
```

## The sample

### Bodu.IO.Pst.Samples.PstBasics

The PST stack from the raw container to the mail-store view, over a Unicode and an ANSI file
side by side:

- **DetectAndOpen** — `PstFile.IsPstFile` signature probing, the open handshake
  (<xref:Bodu.IO.Pst.PstFile.Format> / <xref:Bodu.IO.Pst.PstFile.CryptMethod>), and a node
  census by <xref:Bodu.IO.Pst.PstNodeType> — the one surface serving both file formats.
- **NodesAndProperties** — the two LTP views without MAPI semantics: the message-store node's
  property context (<xref:Bodu.IO.Pst.PstPropertyContext>) and the root folder's hierarchy
  table (<xref:Bodu.IO.Pst.PstTableContext>), whose row identifiers are child-folder NIDs.
- **StreamingAndValidation** — `DataLength` pricing, `OpenDataStream` chunked reads under
  `Strict` validation, the `BlockCacheSize` knob on <xref:Bodu.IO.Pst.PstFileOptions>, and a
  truncated copy rejected inside the <xref:Bodu.IO.Pst.PstFileException> family.
- **ReadMailStore** — the same files through `Bodu.Formats.Outlook.Pst`:
  <xref:Bodu.Formats.Outlook.OutlookMailStore> folders, messages, recipients, attachments,
  bodies, and named-property resolution, then the ANSI store's code-page strings decoded through
  the same accessors.

Point `Program.SamplePath` or `Program.AnsiSamplePath` at any other `.pst` of either format to
explore your own archive.

*Packages: `Bodu.IO.Pst`, `Bodu.Formats.Outlook.Pst`.*

## Related

- [Bodu.IO.Pst introduction](../docs/io-pst/index.md) — the node database and LTP layers the
  sample walks, and the mail-store reader built on them.
- [Outlook guides](../guides/outlook/index.md) — the shared MAPI value model and the `.msg`
  reader that shares it.
- [IO.Compound samples](io-compound.md) — the OLE2 container that backs `.msg`, `.xls`, and
  `.doc` files.
