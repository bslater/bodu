# Bodu.Text.Bencode.Samples.TorrentFile

The flagship `Bodu.Text.Bencode` sample: reading, verifying, and re-authoring a real
BitTorrent metainfo file (BEP 3) — the format Bencode was invented for. Four scenarios climb
the library's layers over one committed 278-byte `Data/sample.torrent`: DOM inspection,
canonical byte-exact round trips, the raw-slice surface that makes info-hashing safe, and
typed POCO mapping. The info-hash scenario also crosses packages, using `Bodu.Text.Encoding`'s
`Base16` to render the digest.

Everything runs offline and deterministically.

```bash
dotnet run --project samples/Text.Bencode/Bodu.Text.Bencode.Samples.TorrentFile
```

## Scenario 1 — ParseTorrent

**Intent.** Show the `JsonDocument`-style entry point for a format you inspect rather than
map: one parse over the raw bytes, then cheap `BencodeElement` cursors — the right layer when
a torrent's exact shape (single-file vs multi-file, optional keys) is discovered as you go.

**What it does.** Parses `Data/sample.torrent` with `BencodeDocument.Parse` and walks the
metainfo dictionary: string keys via `GetString`, the `creation date` unix timestamp via
`GetInt64` (converted to `DateTimeOffset`), and the nested `info` dictionary's payload
fields. Crucially it reads `pieces` — a run of 20-byte SHA-1 hashes — with `GetBytes`, since
Bencode strings are *byte* strings and this one is not text. It finishes by probing the
absent optional `announce-list` with `TryGetProperty`.

**What to expect.** The tracker URL, comment, and creation date; the payload described as
32768 bytes across two 16384-byte pieces; the `pieces` value sized as exactly two hashes; and
`False` for the optional key:

```text
announce      : http://tracker.example.com:6969/announce
comment       : Bodu.Text.Bencode test fixture
creation date : 2025-06-11 00:00Z
info.name     : sample.bin
info.length   : 32768 bytes across 2 pieces of 16384
info.pieces   : 40 bytes = 2 SHA-1 piece hashes
announce-list : present = False
```

**APIs demonstrated.** `BencodeDocument.Parse(byte[])` (+ `IDisposable`), `RootElement`,
`GetProperty` / `TryGetProperty`, `GetString` vs `GetBytes` (text vs binary byte strings),
`GetInt64`.

## Scenario 2 — CanonicalRoundTrip

**Intent.** Demonstrate Bencode's defining property: the encoding is *canonical*. Dictionary
keys must appear in ascending raw-byte order, so every value has exactly one valid encoding.
That is what makes parse → re-emit byte-identical, and it is enforced at both ends of the
pipeline.

**What it does.** Re-emits the parsed torrent through `BencodeElement.WriteTo` +
`Utf8BencodeWriter` and compares against the original file with `SequenceEqual`. It then
shows the writer's side of the contract — entries written out of order (`name` before
`length`) still emit sorted, because each dictionary is re-ordered as it closes, while a
duplicate key throws `BencodeSerializationException` since no valid encoding can contain it.
Finally the reader's side: strict parsing rejects a non-canonical document (`name` key before
`length`) with `BencodeFormatException`, and `BencodeReaderOptions.AllowUnsortedKeys = true`
opts into lenient ingestion of such legacy output.

**What to expect.** A `True` for the 278-byte re-emission, the out-of-order write emerging
sorted as `d6:lengthi2e4:namei1ee`, and the two rejection messages followed by the lenient
parse succeeding:

```text
re-emitted 278 bytes; byte-identical to the file -> True
writer sorts keys on close      -> d6:lengthi2e4:namei1ee
duplicate key rejected on write -> The dictionary contains more than one entry for the key 'name'.
unsorted keys rejected on read  -> Bencoded dictionary keys must be sorted by raw byte order.
AllowUnsortedKeys = true accepts the same input (6 tokens)
```

**APIs demonstrated.** `BencodeElement.WriteTo(Utf8BencodeWriter)`,
`Utf8BencodeWriter(IBufferWriter<byte>)`, dictionary sort-on-close semantics,
`BencodeSerializationException` (duplicate key), `Utf8BencodeReader` strict vs
`BencodeReaderOptions.AllowUnsortedKeys`, `BencodeFormatException`.

## Scenario 3 — InfoHashRawSlice

**Intent.** Solve BitTorrent's most famous requirement — the info-hash is the SHA-1 of the
`info` dictionary's *exact encoded bytes* — and show why the raw-slice surface exists.
Re-serializing a parsed tree risks producing different bytes in a non-canonical format;
because Bencode is canonical and `GetRawBytes` returns the element's original slice, the
hash is computed with zero drift risk.

**What it does.** Pulls the `info` element's complete encoded form with `GetRawBytes` (109 of
the file's 278 bytes), hashes it with `SHA1.HashData`, and renders the digest with
`Base16.Encode` from `Bodu.Text.Encoding` — the lowercase hex form trackers display. It then
re-authors the torrent for a new tracker by writing a fresh document that splices the
untouched slice in verbatim via `WritePropertyName("info")` + `WriteRawValue`, re-parses it,
and proves the info-hash is unchanged.

**What to expect.** The slice size, the 40-hex-character info-hash, the new announce URL,
and confirmation the hash survived re-authoring:

```text
info slice : 109 bytes of the 278-byte file
info-hash  : f98cd9393539251cfeea8745e7a56031c84236ee
re-authored: new announce 'http://mirror.example.org:6969/announce'
hash intact: True
```

**APIs demonstrated.** `BencodeElement.GetRawBytes()`, `Utf8BencodeWriter.WriteRawValue`,
`WritePropertyName`, cross-package `Base16.Encode(bytes, Base16Variant.Lower)`,
`SHA1.HashData`.

## Scenario 4 — PocoTorrent

**Intent.** Show the typed layer for when the shape *is* known: `BencodeSerializer` maps the
metainfo dictionary onto a POCO graph in one call. Torrent keys include spaces
(`creation date`, `piece length`) — precisely what `[BencodePropertyName]` exists for — and
binary values bind to `byte[]`.

**What it does.** Defines `TorrentMeta` / `TorrentInfo` POCOs whose properties carry explicit
wire names, deserializes the file with `BencodeSerializer.Deserialize<TorrentMeta>`, and
prints the typed values. It then serializes the graph back and compares against the original
file — canonical encoding makes even the POCO round trip byte-exact.

**What to expect.** The typed fields, and a byte-identical 278-byte re-encoding:

```text
announce   : http://tracker.example.com:6969/announce
created by : Bodu fixture generator at 2025-06-11
payload    : sample.bin (32768 bytes, 2 pieces)
round trip : 278 bytes, byte-identical -> True
```

**APIs demonstrated.** `BencodeSerializer.Deserialize<T>(byte[])`,
`BencodeSerializer.Serialize<T>`, `[BencodePropertyName]` (keys with spaces), `byte[]`
binding for binary byte strings, nested POCO mapping.

## Layout

```text
Bodu.Text.Bencode.Samples.TorrentFile/
  Program.cs                       # runs the scenarios in order
  Data/sample.torrent              # committed single-file metainfo fixture (278 bytes)
  Scenarios/ParseTorrent.cs
  Scenarios/CanonicalRoundTrip.cs
  Scenarios/InfoHashRawSlice.cs
  Scenarios/PocoTorrent.cs
```

## Related

- `Bodu.Text.Toml` samples (`samples/Text.Toml/`) — the same System.Text.Json-shaped stack
  (serializer / mutable DOM / read-only DOM / token layer) for TOML.
- `Bodu.Text.Encoding` samples (`samples/Text.Encoding/`) — the `Base16` surface used here,
  and the rest of the binary-encoding catalogue.
- Guides: `docs/guides/serialization/bencode/`.
