# Text.Bencode Samples

A console application demonstrating the `Bodu.Text.Bencode` package against the format's
canonical use case. Run it with:

```bash
dotnet run --project samples/Text.Bencode/Bodu.Text.Bencode.Samples.TorrentFile
```

The sample is offline and deterministic: every scenario reads the committed 278-byte
`Data/sample.torrent` fixture.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Text.Bencode.Samples.TorrentFile` | A real BitTorrent metainfo file (BEP 3) end to end — `BencodeDocument` DOM inspection (`GetString` vs `GetBytes` for text vs binary), canonical byte-exact round trips with writer sort-on-close / duplicate rejection and strict-vs-lenient reader key ordering, the info-hash computed from `GetRawBytes()` and spliced verbatim with `WriteRawValue`, and `BencodeSerializer` POCO mapping with `[BencodePropertyName]` for keys containing spaces | `Bodu.Text.Bencode`, `Bodu.Text.Encoding` (Base16 info-hash rendering) |
