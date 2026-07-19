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
utf8-bom.txt    : UTF-8-BOM              BOM=3B  text="Hello, Bodu café"
utf16le-bom.txt : UTF-16LE-BOM           BOM=2B  text="Hello, Bodu café"
utf16be-bom.txt : UTF-16BE-BOM           BOM=2B  text="Hello, Bodu café"
plain-utf8.txt  : (no BOM)               fallback UTF-8   text="Hello, Bodu café"
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
UTF-16 bytes     : 32
-> UTF-8 bytes   : 17 (é is 2 bytes in UTF-8)
-> back to UTF-16: 32  round-trips: True
UTF-8 +preamble  : 20 bytes (HasPreamble=True, preamble=3B)
after StripPreamble: 17 bytes
ASCII replacement: "Hello, Bodu caf?"
ASCII exception  : threw EncoderFallbackException on 'é' (UsesExceptionFallbacks=True)
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
Phrase length    : 16 chars
UTF-8 byte count : 17
UTF-16 byte count: 32
TryEncodeUtf8To  : ok=True, bytesWritten=17
ToUtf8Bytes match: True
GetUtf8BytesPooled: WrittenCount=17, matches=True
```

**APIs demonstrated.** `StringEncodingExtensions.GetUtf8ByteCount`, `.GetEncodedByteCount`,
`.TryEncodeUtf8To`, `.ToUtf8Bytes`, `.GetUtf8BytesPooled` (returning a pooled `PooledBufferBuilder<byte>`).

## Layout

```text
Bodu.Core.Samples.TextEncoding/
  Program.cs                        # runs the scenarios in order
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
