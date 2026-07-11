---
title: Runnable samples
---

# Runnable samples

The repository ships a runnable, self-contained sample project for `Bodu.Text.Bencode` under
[`samples/Text.Bencode/`](https://github.com/bslater/bodu/tree/master/samples/Text.Bencode).
It is **offline and deterministic** — every scenario reads one committed 278-byte
`Data/sample.torrent` fixture — and is a member of `bodu.slnx`, built and executed by CI. The
README documents every scenario individually: its intent, what the code does, the output to
expect, and the APIs demonstrated.

Run it from the repository root:

```bash
dotnet run --project samples/Text.Bencode/Bodu.Text.Bencode.Samples.TorrentFile
```

## The sample

### Bodu.Text.Bencode.Samples.TorrentFile

A real BitTorrent metainfo file (BEP 3) — the format Bencode was invented for — read,
verified, and re-authored through all of the library's layers:

- **ParseTorrent** — the read-only <xref:Bodu.Text.Bencode.Document.BencodeDocument> DOM:
  one parse, cheap <xref:Bodu.Text.Bencode.Document.BencodeElement> cursors, `GetString` vs
  `GetBytes` for text vs binary byte strings (the `pieces` SHA-1 run), and `TryGetProperty`
  probing for optional keys.
- **CanonicalRoundTrip** — Bencode's defining property: keys ascend in raw-byte order, so a
  value has exactly one encoding. Parse → re-emit reproduces the file byte for byte; the
  <xref:Bodu.Text.Bencode.Writer.Utf8BencodeWriter> sorts each dictionary as it closes and
  rejects duplicates; the strict <xref:Bodu.Text.Bencode.Reader.Utf8BencodeReader> rejects
  unsorted input unless <xref:Bodu.Text.Bencode.Reader.BencodeReaderOptions.AllowUnsortedKeys>
  opts into lenient ingestion.
- **InfoHashRawSlice** — the info-hash is the SHA-1 of the `info` dictionary's *exact encoded
  bytes*: `BencodeElement.GetRawBytes()` hands back precisely that slice, and
  `Utf8BencodeWriter.WriteRawValue` splices it untouched into a re-authored torrent, proving
  the hash survives. The digest renders through `Bodu.Text.Encoding`'s `Base16` — a
  cross-package demonstration.
- **PocoTorrent** — the typed layer: <xref:Bodu.Text.Bencode.BencodeSerializer> maps the
  metainfo onto POCOs whose `[BencodePropertyName]` attributes carry the keys with spaces
  (`creation date`, `piece length`), with `byte[]` binding for binary values — and even the
  POCO round trip is byte-exact.

*Packages: `Bodu.Text.Bencode`, `Bodu.Text.Encoding` (info-hash rendering).*

## Guarded documentation

The Bencode guides under
[`docs/guides/serialization/bencode/`](../guides/serialization/bencode/index.md) carry
compile-guarded snippets: examples marked with a `<!-- compile -->` sentinel are compiled
against the current public API by `DocumentationSnippetCompileTests` in the library's test
project (Regression tier).

## Related

- [Bencode guides](../guides/serialization/bencode/index.md) — the full serializer, DOM, and
  reader/writer documentation.
- [TOML samples](toml.md) — the same System.Text.Json-aligned stack for TOML.
- [Text.Encoding samples](text-encoding.md) — the `Base16` surface used by the info-hash
  scenario.
