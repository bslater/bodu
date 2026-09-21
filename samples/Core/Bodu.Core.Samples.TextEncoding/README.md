# Bodu.Core.Samples.TextEncoding

The `Bodu.Text` encoding surfaces from `Bodu.Core`: `EncodingDetection` for byte-order-mark sniffing,
`EncodingExtensions` for transcoding, preamble handling and fallback selection, and
`StringEncodingExtensions` for pooled and span-based string encoding. Three scenarios read committed
byte fixtures under `Data/`, so the output is identical on every run.

Everything runs offline with fixed inputs — deterministic output every run.

```bash
dotnet run --project samples/Core/Bodu.Core.Samples.TextEncoding
```

The `Data/` fixtures all hold the same phrase — `Hello, Bodu café` — written four ways: UTF-8 with a
BOM, UTF-16 little-endian with a BOM, UTF-16 big-endian with a BOM, and plain UTF-8 with no BOM. The
`é` (U+00E9) is what makes the byte counts differ between encodings.

## Scenario 1 — DetectEncoding

**Intent.** Show `EncodingDetection.TryDetectByPreamble` naming a stream's encoding from its leading
byte-order-mark alone, and how a caller handles the BOM-less case.

**What it does.** Reads each fixture's raw bytes, sniffs the preamble, and — on a hit — strips the
BOM and decodes the payload back to text. The BOM-less file misses detection, so the sample decodes
it with an explicit UTF-8 fallback.

**What to expect.** The three BOM fixtures are identified (note the differing preamble lengths — 3
bytes for UTF-8, 2 for UTF-16), and every payload decodes back to the original phrase. The BOM-less
file reports `(no BOM)` and takes the UTF-8 fallback:

```text
--- EncodingDetection - byte-order-mark sniffing ---
  What   : Writes the same phrase four ways - UTF-8, UTF-16LE and UTF-16BE with byte-order marks, and plain UTF-8
           without one - then detects each and decodes it.
  Why    : A byte stream carries no declaration of its encoding, so something has to decide. A BOM is the one
           reliable in-band signal, and reading it is cheap and unambiguous. Getting this wrong is not subtle:
           decode UTF-16LE as UTF-8 and every character comes back interleaved with nulls, or leave a UTF-8 BOM in
           place and the first field of a CSV silently begins with an invisible character that breaks an exact-match
           comparison.
  Expect : All four decode to the identical string, which is the point - the difference lives in the bytes, not the
           text. The BOM lengths differ (3 for UTF-8, 2 for either UTF-16), and the unmarked file falls back to
           UTF-8 rather than failing.

  utf8-bom.txt    : UTF-8-BOM              BOM=3B  text="Hello, Bodu café"  (detected from the leading bytes alone, before any decoding was attempted)
  utf16le-bom.txt : UTF-16LE-BOM           BOM=2B  text="Hello, Bodu café"  (detected from the leading bytes alone, before any decoding was attempted)
  utf16be-bom.txt : UTF-16BE-BOM           BOM=2B  text="Hello, Bodu café"  (detected from the leading bytes alone, before any decoding was attempted)
  plain-utf8.txt  : (no BOM)               fallback UTF-8   text="Hello, Bodu café"  (no signal to read, so the caller's fallback decides - UTF-8 is the safe modern default)
```

> Note: `EncodingDetection` exposes only BOM-based detection (`TryDetectByPreamble`) — there is no
> content-scanning heuristic in the type, so the BOM-less branch decides its own default rather than
> guessing from the bytes.

**APIs demonstrated.** `EncodingDetection.TryDetectByPreamble`, `EncodingExtensions.StripPreamble`,
`EncodingExtensions.GetPreambleLength`, `EncodingExtensions.GetDisplayName`.

## Scenario 2 — TranscodeAndFallback

**Intent.** Show byte-level re-encoding without an intermediate string (`Transcode`), preamble
round-tripping, and the choice between the replacement and exception fallback policies.

**What it does.** Encodes the phrase to UTF-16LE, transcodes those bytes to UTF-8 and back, and
checks the round-trip. It then emits a BOM-prefixed UTF-8 blob and strips it. Finally it targets
ASCII — which cannot represent `é` — once with a replacement fallback and once with an exception
fallback.

**What to expect.** UTF-16 is 32 bytes (two per code unit) while UTF-8 is 17 (the `é` costs two
bytes); the round-trip is exact. The preamble adds 3 bytes and strips cleanly. The replacement policy
yields `caf?`; the exception policy throws on the `é`:

```text
--- EncodingExtensions - transcode, preamble, fallback ---
  What   : Transcodes a phrase between UTF-16 and UTF-8 and back, adds and strips a preamble, then encodes a
           non-ASCII character to ASCII under both the replacement and the exception fallback.
  Why    : The fallback choice is the one that matters, because the two behaviours fail in opposite directions.
           Replacement never throws and silently substitutes '?' - fine for a log line, catastrophic for a name, an
           identifier or anything that will be compared or stored. The exception fallback refuses instead, telling
           you which character it could not represent. Defaulting to replacement is how mojibake gets written to a
           database and only noticed later.
  Expect : UTF-16 takes 32 bytes to UTF-8's 17 for the same 16 characters, because ASCII costs one byte in UTF-8 and
           two in UTF-16, and the round trip is byte-identical. Under ASCII, the same input either becomes "caf?" or
           throws EncoderFallbackException naming the character.

  UTF-16 bytes     : 32  (expected 32 - two bytes per character, whether or not the character needs them)
  -> UTF-8 bytes   : 17  (expected 17 - one byte per ASCII character plus two for the é; almost half the size for this text)
  -> back to UTF-16: 32  round-trips: True  (expected True - both encodings cover the whole of Unicode, so transcoding between them loses nothing)
  UTF-8 +preamble  : 20 bytes  (3 more than the text: the preamble is data, and it is why an unstripped file starts with an invisible character)
  after StripPreamble: 17 bytes  (back to 17 - strip before comparing or parsing, never after)
  ASCII replacement: "Hello, Bodu caf?"  (the é became ? and nothing was raised - silent, irreversible, and the default)
  ASCII exception  : threw EncoderFallbackException on 'é'  (the same input, refused rather than mangled, and it names the offending character)
```

**APIs demonstrated.** `EncodingExtensions.Transcode`, `.GetBytesWithPreamble`, `.StripPreamble`,
`.HasPreamble`, `.GetPreambleLength`, `.WithReplacementFallbacks`, `.WithExceptionFallbacks`,
`.UsesExceptionFallbacks`.

## Scenario 3 — PooledStringEncoding

**Intent.** Show the string-side encoding helpers: size a buffer exactly, encode into a caller-owned
span with no allocation, and rent the output buffer from the shared pool.

**What it does.** Probes the UTF-8 and UTF-16 byte counts, encodes into a `stackalloc` span via
`TryEncodeUtf8To`, compares against the allocating `ToUtf8Bytes`, then encodes into a pooled buffer
whose rented storage is returned by `using`.

**What to expect.** The 16-character phrase is 17 UTF-8 bytes (the `é`) and 32 UTF-16 bytes; the
span, allocated, and pooled encodings all agree byte-for-byte:

```text
--- StringEncodingExtensions - sizing, span, and pooled encoding ---
  What   : Measures a phrase in both encodings, encodes it into a stack buffer through a Try pattern, and again into
           a pooled buffer, checking all three agree byte for byte.
  Why    : Encoding a string normally allocates a byte array per call, which on a hot path is garbage generated for
           data that is consumed and discarded immediately. Sizing first lets the caller supply the memory: a stack
           buffer when the bound is small and known, a pooled one when it is not. The Try form matters because it
           reports a buffer too small rather than throwing, which is the difference between a fallback path and an
           exception on a hot loop.
  Expect : 17 UTF-8 bytes against 32 UTF-16 for 16 characters. All three routes produce identical bytes - that
           equality is the claim, since a pooled buffer is longer than its content and only WrittenSpan is
           meaningful.

  Phrase length    : 16 chars  (characters, not bytes - the two differ the moment the text leaves ASCII)
  UTF-8 byte count : 17  (expected 17 - measured before encoding, which is what lets the caller own the buffer)
  UTF-16 byte count: 32  (expected 32 - the same text costs nearly twice as much in the encoding .NET uses in memory)
  TryEncodeUtf8To  : ok=True, bytesWritten=17  (expected True and 17 - a short buffer would return False here rather than throw, so a caller can fall back)
  ToUtf8Bytes match: True  (expected True - the allocating convenience form and the stack form must not diverge)
  GetUtf8BytesPooled: WrittenCount=17, matches=True  (the rented array is longer than the content, so read WrittenSpan and never the whole buffer)
```

**APIs demonstrated.** `StringEncodingExtensions.GetUtf8ByteCount`, `.GetEncodedByteCount`,
`.TryEncodeUtf8To`, `.ToUtf8Bytes`, `.GetUtf8BytesPooled` (returning a pooled `PooledBufferBuilder<byte>`).

## Layout

```text
Bodu.Core.Samples.TextEncoding/
  Program.cs                        # runs the scenarios in order
  SampleConsole.cs                  # the what/why/expect banner every scenario opens with
  Data/utf8-bom.txt                 # "Hello, Bodu café" — UTF-8 with BOM
  Data/utf16le-bom.txt              #                    — UTF-16LE with BOM
  Data/utf16be-bom.txt              #                    — UTF-16BE with BOM
  Data/plain-utf8.txt               #                    — UTF-8, no BOM
  Scenarios/DetectEncoding.cs
  Scenarios/TranscodeAndFallback.cs
  Scenarios/PooledStringEncoding.cs
```

## Related

- `Bodu.Core.Samples.CoreToolbox` — sequences, pooled buffers, the enumerable operators, string and
  numeric extensions, `WeekPattern`, and the async threading primitives.
- `Bodu.Core.Samples.FunctionalRailway` — the `Bodu.Functional` seam: `Option<T>`, `Result`,
  `Either<,>`, and `Memoizer`.
```
