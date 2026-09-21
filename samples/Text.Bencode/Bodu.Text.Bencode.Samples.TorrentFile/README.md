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
--- Reading a .torrent with the read-only BencodeDocument DOM ---
  What   : Parses the committed torrent once, then walks the metainfo dictionary through cheap element cursors:
           tracker URL, creation timestamp, the info dictionary's payload description, the raw piece hashes, and a
           safe probe for a key this fixture does not have.
  Why    : This is the layer for a file whose exact shape you discover as you read it. A torrent is a dictionary of
           mostly-optional keys, so binding it to a fixed type up front means either a schema that rejects real
           files or one padded with nullables. The document DOM parses once and hands out cursors into that single
           buffer, so walking it costs no further allocation. The other reason to read at this layer is Bencode's
           byte-string model: a value is a length-prefixed run of bytes, and whether it is text or binary is
           something only the spec tells you - the encoding does not.
  Expect : Every field resolves from one parse. Note the split between GetString for the text keys and GetBytes for
           'pieces': that value is a concatenation of 20-byte SHA-1 hashes, and decoding it as UTF-8 would corrupt
           it. TryGetProperty reports the absent key as False rather than throwing, which is how optional metainfo
           keys are meant to be probed.

  announce      : http://tracker.example.com:6969/announce  (a text byte string - GetString decodes it as UTF-8)
  comment       : Bodu.Text.Bencode test fixture
  creation date : 2025-06-11 00:00Z  (Bencode has one numeric type, a signed integer - there is no date type, so the wire value is a unix timestamp)
  info.name     : sample.bin
  info.length   : 32768 bytes across 2 pieces of 16384
  info.pieces   : 40 bytes = 2 SHA-1 piece hashes  (read with GetBytes, never GetString - this is binary, and a UTF-8 decode would silently corrupt it)
  announce-list : present = False  (expected False - most metainfo keys are optional, so probing beats catching)
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
--- Canonical form - one value, exactly one encoding ---
  What   : Re-emits the parsed torrent and compares it to the file byte for byte, then shows the writer sorting keys
           as it closes a dictionary, rejecting a duplicate key, and the reader refusing unsorted input unless the
           lenient option is set.
  Why    : Bencode requires a dictionary's keys to appear in ascending raw-byte order, which means a value has one
           valid encoding rather than many. That is not a stylistic rule - it is what lets BitTorrent identify a
           torrent by the SHA-1 of its info dictionary. If two encoders could disagree about key order or integer
           padding, the same torrent would hash to different values and the network would treat it as two different
           files. Canonical form is what makes hashing a parsed-and-re-emitted structure safe at all.
  Expect : The re-emitted bytes equal the file exactly, so nothing about the original encoding was lost in the round
           trip. The writer emits 'length' before 'name' although they were written in the other order, because it
           sorts on close rather than trusting the caller. A duplicate key and unsorted input are both errors, since
           neither can produce a canonical encoding - the lenient reader option exists for ingesting data someone
           else got wrong, not for writing it.

  re-emitted 278 bytes; byte-identical to the file -> True  (expected True - the property that makes hashing a re-emitted structure safe)
  writer sorts keys on close      -> d6:lengthi2e4:namei1ee  ('length' emits before 'name' despite the write order - the caller cannot produce non-canonical output by accident)
  duplicate key rejected on write -> The dictionary contains more than one entry for the key 'name'.  (no key order can encode a duplicate, so the writer fails rather than emitting something unparseable)
  unsorted keys rejected on read  -> Bencoded dictionary keys must be sorted by raw byte order.  (the reader holds the same contract by default, so a non-canonical file cannot pass silently)
  AllowUnsortedKeys = true accepts the same input (6 tokens)  (the opt-out for ingesting data another tool encoded wrong - re-emitting it will produce the canonical order)
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
--- Info-hash - hashing an element's exact encoded bytes ---
  What   : Takes the raw encoded slice of the 'info' dictionary straight out of the parsed document, hashes it, then
           splices that untouched slice into a freshly authored torrent with a different tracker and re-hashes the
           result.
  Why    : BitTorrent identifies a torrent by the SHA-1 of its info dictionary's encoded bytes, so the hash is over
           a byte range rather than over a value. Recomputing it by re-serializing a parsed object would be a bet
           that the serializer reproduces the original encoding exactly - true here because Bencode is canonical,
           but a bet nonetheless, and false for most formats. GetRawBytes removes the bet: it returns the bytes that
           were actually in the file. WriteRawValue is the same idea on the writing side, letting a document carry a
           foreign sub-structure through unmodified.
  Expect : The info slice is a subset of the file - the tracker URL and comment sit outside it, which is exactly why
           re-authoring those does not change the identity. After the rewrite the new document carries a different
           announce URL and an unchanged info-hash, which is what makes mirroring a torrent to another tracker
           possible.

  info slice : 109 bytes of the 278-byte file  (a slice of the parsed buffer, not a re-serialization - these are the bytes BEP 3 says to hash)
  info-hash  : f98cd9393539251cfeea8745e7a56031c84236ee  (the torrent's identity on the network - every peer must derive this same value)
  re-authored: new announce 'http://mirror.example.org:6969/announce'  (a different tracker - announce lives outside the info dictionary, so it is not part of the identity)
  hash intact: True  (expected True - the slice was copied through verbatim, so peers still recognise the same torrent)
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
--- Typed layer - BencodeSerializer onto a POCO graph ---
  What   : Deserializes the same torrent into a two-class object graph, reads the fields as ordinary properties, and
           serializes the graph back to compare against the original file.
  Why    : Once the shape is known, a typed model beats cursor-walking: the keys are validated in one place, the
           values arrive as the right CLR types, and the rest of the program is ordinary C#. Two things are worth
           noticing in the model. Torrent keys contain spaces, so they cannot be C# identifiers - [PropertyName]
           carries the wire name, which is the mechanism for every format whose keys are not identifiers. And
           'pieces' binds to byte[] rather than string, because Bencode byte strings are not text; choosing string
           there would corrupt the hashes on the way in and again on the way out.
  Expect : The re-serialized bytes equal the original file. That is a stronger result than it looks - it means the
           serializer emitted the same canonical key order and preserved the binary value intact, so a torrent can
           be deserialized, inspected, and written back without changing its info-hash.

  announce   : http://tracker.example.com:6969/announce
  created by : Bodu fixture generator at 2025-06-11
  payload    : sample.bin (32768 bytes, 2 pieces)  (Pieces bound to byte[], so the 20-byte SHA-1 hashes survive the trip through the object model)
  round trip : 278 bytes, byte-identical -> True  (expected True - deserialize, inspect and re-serialize without changing the torrent's identity)
```

**APIs demonstrated.** `BencodeSerializer.Deserialize<T>(byte[])`,
`BencodeSerializer.Serialize<T>`, `[BencodePropertyName]` (keys with spaces), `byte[]`
binding for binary byte strings, nested POCO mapping.

## Layout

```text
Bodu.Text.Bencode.Samples.TorrentFile/
  Program.cs                       # runs the scenarios in order
  SampleConsole.cs                 # the What / Why / Expect scenario banner
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
