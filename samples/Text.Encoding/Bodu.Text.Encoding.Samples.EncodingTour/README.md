# Bodu.Text.Encoding.Samples.EncodingTour

A guided tour of the `Bodu.Text.Encoding` catalogue: the base families
(Base16/32/45/58/62/64/85) and their published variants, the formatting and parse-style
option enums, the checksummed schemes built for identifiers humans re-type (Base58Check,
Bech32), the `Guid` convenience overloads, and the name-addressable `BinaryEncodings`
registry. Every scenario is pure computation over fixed payloads — offline and
deterministic, no data files.

> Note on namespaces: the sample's root namespace is `Bodu.Samples.Text.Encoding.*`. A
> namespace under `Bodu.Text` would also work here, but the samples avoid it uniformly since
> some sibling packages' facade classes (e.g. `Delimited`) are shadowed by their namespaces
> from inside `Bodu.Text`.

```bash
dotnet run --project samples/Text.Encoding/Bodu.Text.Encoding.Samples.EncodingTour
```

## Scenario 1 — VariantsTour

**Intent.** Map the catalogue: one payload through every base family shows the trade-off
each makes (length vs alphabet safety vs padding), and one family through its variants shows
that the alphabet is a *parameter* — switching RFC 4648 Base32 to Crockford, or Ascii85 to
Z85, is one enum argument on the same API.

**What it does.** Encodes the 5-byte payload `Bodu!` through Base16, Base32, Base45, Base58,
Base62, Base64, and Base85; then re-encodes it through the four `Base32Variant`s and two
`Base85Variant`s (Z85 gets a 4-byte payload — it requires 4-byte alignment); and finally
decodes Crockford output with the matching variant to underline that alphabets are not
interchangeable.

**What to expect.**

```text
--- One payload, every family and variant ---
  What   : Encodes the same five bytes with each base family, then re-encodes one payload through four Base32
           alphabets and two Base85 alphabets, and decodes a Crockford string back with the matching variant.
  Why    : A binary-to-text encoding exists to move bytes through a channel that only carries text - a URL, a JSON
           string, an email header, a QR code. The families differ in how much they cost you: Base16 doubles the
           size and is trivially readable, Base64 is about 4/3 and is the default almost everywhere, Base85 is about
           5/4 but uses punctuation that many channels mangle. The variants exist because the alphabet is a channel
           decision, not an algorithm one - Crockford drops the characters humans confuse, URL-safe Base64 drops the
           two that need percent-escaping. Here they are one enum argument rather than a different API.
  Expect : Encoded length grows as the alphabet shrinks, which is the whole trade. The four Base32 rows encode
           identical bytes to visibly different text - the alphabet is part of the contract, so decoding with the
           wrong variant yields wrong bytes or an error rather than a helpful guess. Z85 is shown over four bytes
           because it requires 4-byte alignment.

  payload  : 5 bytes 'Bodu!'  (one fixed input, so every row below differs only by the encoding applied to it)
  Base16   : 426f647521  (two characters per byte - the most expensive and the easiest to read by eye)
  Base32   : IJXWI5JB
  Base45   : .H8MVCX0
  Base58   : 8VjE9ma
  Base62   : 51SNhHl
  Base64   : Qm9kdSE=
  Base85   : 6>pCW+T  (the most compact here, at the cost of punctuation many channels escape or mangle)

  Base32 Standard  : IJXWI5JB
  Base32 HexExt    : 89NM8T91
  Base32 Crockford : 89QP8X91  (excludes I, L, O and U, so a human re-typing the value cannot confuse them with 1 and 0)
  Base32 ZBase32   : ejzse7jb
  Base85 Ascii85   : 6>pCW+T
  Base85 Z85       : ?MsJX (Z85 needs 4-byte alignment)

  decode with matching variant: True  (expected True - and note the variant must be passed again, because the alphabet is not recoverable from the text)
```

**APIs demonstrated.** The per-family `Encode`/`Decode` statics, `Base32Variant.Standard` /
`.HexExtended` / `.Crockford` / `.ZBase32`, `Base85Variant.Ascii85` / `.Z85`.

## Scenario 2 — FormattingAndStyles

**Intent.** Show the two option enums that bracket every codec. `BaseFormattingOptions`
shapes the text you *produce* (case, `0x` prefixes, byte spacing, padding omission);
`BaseFormatStyles` declares what you *tolerate* when parsing text someone else produced.
Strict by default, lenient by explicit opt-in — the same philosophy as the rest of the
solution's parsers.

**What it does.** Encodes one payload with `UpperCase`, `IncludePrefix`, `InsertSpacing`,
and (for Base64) `OmitPadding`; then parses the decorated string `0xDE AD BE EF 01 23` —
rejected by strict `IsValid`, recovered by `AllowPrefix | IgnoreWhitespace` — and re-parses
unpadded Base64 with `AllowMissingPadding`.

**What to expect.**

```text
--- Formatting on write, tolerance on read ---
  What   : Encodes one payload with each formatting option - case, prefix, spacing, omitted padding - then parses a
           decorated string that strict parsing rejects, and an unpadded one, by naming exactly which deviations to
           accept.
  Why    : These are two different questions and the API keeps them apart. What you emit should be as close to
           canonical as the consumer allows, because every decoration is something a downstream parser has to be
           taught about. What you accept is a separate decision, made once per input source: a hex dump pasted from
           a debugger carries 0x and spaces, a JWT segment has its padding stripped, and neither is your bug to
           reject on principle. The failure mode this design avoids is a parser that is quietly permissive about
           everything, where a typo that should have been an error becomes wrong bytes instead.
  Expect : Each formatting option changes only the presentation - decode any of these rows and the same six bytes
           come back. Strict parsing rejects the decorated input rather than guessing, and the same input succeeds
           once the two tolerated deviations are named. Tolerance is opt-in per call, so widening it for one
           untrusted source does not widen it everywhere.

  default        : deadbeef0123  (the canonical form - what to emit unless a consumer demands otherwise)
  UpperCase      : DEADBEEF0123
  IncludePrefix  : 0xdeadbeef0123
  InsertSpacing  : de ad be ef 01 23
  OmitPadding    : 3q2+7wEj  (no trailing '=' - the convention JWTs and URL fragments use, since the length implies the padding)

  input '0xDE AD BE EF 01 23'
  strict parse rejects   : True  (expected True - by default the prefix and spaces are errors, not noise to be skipped)
  AllowPrefix|IgnoreWs   : 6 bytes recovered -> True  (expected True - naming the two deviations accepts them without accepting anything else)
  AllowMissingPadding    : '3q2+7wEj' -> 6 bytes  (the read-side counterpart of OmitPadding - the two options are how a codec round-trips through a padding-hostile channel)
```

**APIs demonstrated.** `BaseFormattingOptions.UpperCase` / `.IncludePrefix` /
`.InsertSpacing` / `.OmitPadding`, `BaseFormatStyles.AllowPrefix` / `.IgnoreWhitespace` /
`.AllowMissingPadding`, `Base16.IsValid`.

## Scenario 3 — ChecksummedSchemes

**Intent.** Introduce the schemes designed for identifiers humans read aloud and re-type:
Base58Check (Bitcoin addresses — a 4-byte double-SHA-256 checksum appended before encoding)
and Bech32 (BIP 173 — a BCH error-detecting code plus a human-readable part). The point is
what happens on corruption: decode *fails*, instead of silently returning wrong bytes.

**What it does.** Encodes a 10-byte payload with both schemes, flips the last character of
each, and shows both decoders throwing `FormatException` with a checksum-verification
message; then decodes the intact Bech32 string, recovering the `hrp`, the payload, and the
detected encoding variant.

**What to expect.**

```text
--- Checksummed schemes - encodings that refuse corrupted input ---
  What   : Encodes one payload as a Base58Check string and as a Bech32 string, alters the final character of each,
           and attempts to decode both; then decodes the intact Bech32 string back to its human-readable part and
           bytes.
  Why    : A plain encoding has no opinion about whether its input is the string you meant. Change a character in a
           Base58 address and you usually get a different, perfectly valid byte sequence - which for a payment
           address means funds sent somewhere unrecoverable. These schemes add a checksum inside the encoding so
           that mistake becomes a decode failure instead. Bech32 goes further with a BCH code that is designed to
           catch the specific error patterns humans and OCR produce, plus a human-readable prefix so a string
           carries what network it belongs to.
  Expect : Both corrupted strings are rejected with a FormatException rather than returning plausible wrong bytes -
           that difference is the entire point of the scheme. The intact string round-trips to the original payload
           and reports its prefix and which Bech32 variant encoded it.

  Base58Check : 12hq5aKRGagYDkza8o7  (payload plus a 4-byte double-SHA-256 checksum, then Base58 over the whole thing)
  corrupt last char -> Base58Check checksum verification failed.  (rejected, not silently decoded to different bytes - the failure mode a plain Base58 string does not have)
  Bech32      : sample1qq2828nkaqver9k5nfpnq4  (the 'sample' prefix before the separator names the context, so a string cannot be used on the wrong network)
  corrupt last char -> Bech32 checksum verification failed.  (the BCH code is tuned for the errors people actually make re-typing or scanning a value)
  intact decode     -> hrp 'sample', 10 bytes, Bech32 (True)  (expected True - an untouched string returns its prefix, its payload and which Bech32 variant produced it)
```

**APIs demonstrated.** `Base58Check.Encode` / `.Decode`, `Bech32.EncodeFromBytes` /
`.DecodeToBytes` (hrp + data + `Bech32Encoding` out), checksum failure as `FormatException`.

## Scenario 4 — GuidConvenience

**Intent.** Show the `Guid` overloads: identifiers destined for URLs, file names, or log
lines encode directly — no `ToByteArray` plumbing — and the base choice sets the length:
36 chars as a standard Guid string, down to 22 in Base58/Base64-UrlSafe.

**What it does.** Encodes one fixed Guid through `Base16.Encode(Guid)`,
`Base32.Encode(Guid, Crockford)`, `Base58.Encode(Guid)`, and
`Base64.Encode(Guid, UrlSafe)`, printing each length, then round-trips the Base58 form with
`DecodeGuid`.

**What to expect.**

```text
--- Guid overloads - shorter identifiers without byte plumbing ---
  What   : Encodes one fixed Guid through the families that ship Guid overloads, printing each result beside its
           character count, then decodes the Base58 form back to the original value.
  Why    : A Guid is sixteen bytes, but its usual text form spends 36 characters on them. That matters wherever
           identifiers are pasted, typed, logged or put in a path - a shorter form is less to wrap, less to mistype,
           and cheaper in a URL. The overloads exist so this does not require round-tripping through ToByteArray by
           hand, which is where endianness bugs get introduced: Guid's in-memory byte order is not its string order,
           and hand-rolled conversions routinely encode one and decode the other.
  Expect : The same value in five widths, from 36 characters down to 22. Base58 and URL-safe Base64 are the two
           worth reaching for - Base58 for an identifier a human may re-type, URL-safe Base64 when it only has to
           survive a query string. The round trip returns the original Guid, so nothing about byte order was lost.

  Guid.ToString()   : 8f3b2b6e-4a41-4a83-9c2e-1d6f5a0b9c47 (36 chars)  (the baseline every row below is shortening)
  Base16            : 6e2b3b8f414a834a9c2e1d6f5a0b9c47 (32 chars)
  Base32 Crockford  : DRNKQ3T19A1MN71E3NQNM2WW8W (26 chars)
  Base58            : Ec3Gar2msRfdcg6YfaZDj4 (22 chars)  (the shortest here, and its alphabet omits the characters people confuse when re-typing)
  Base64 UrlSafe    : bis7j0FKg0qcLh1vWgucRw (22 chars)  (swaps + and / for - and _, so the value needs no percent-escaping in a URL)
  round trip        : True  (expected True - the overloads fix the byte order on both sides, which is where hand-rolled conversions go wrong)
```

**APIs demonstrated.** The `Encode(Guid, ...)` overloads on Base16/32/58/64,
`Base58.DecodeGuid`.

## Scenario 5 — EncodingRegistry

**Intent.** Show `BinaryEncodings`, the name-addressable registry: when the codec is chosen
at runtime (a config value, protocol header, CLI flag), `Get(name)` returns an
`IBinaryEncoding` and the consuming code stays codec-agnostic — the same interface a custom
encoding implements (see the CustomEncoding sample).

**What it does.** Looks up five encodings by their registered names, drives them through the
shared `Encode` and prints each instance's `Name` and `Description`; then uses `IsValid` on
the base58 instance to show alphabet checking on untrusted input (Bitcoin Base58 excludes
`0`, `O`, `I`, `l`).

**What to expect.**

```text
--- BinaryEncodings registry - choosing the codec at runtime ---
  What   : Resolves five encodings by name from the registry, encodes through the shared IBinaryEncoding interface
           without naming a concrete type, and then validates two strings against the Base58 alphabet.
  Why    : The codec is often not a compile-time decision: it arrives in a config file, a protocol header or a
           command-line flag. Without a registry that turns into a switch statement that has to be extended every
           time the catalogue grows, and which silently does the wrong thing for a name it does not recognise.
           Resolving by name keeps the consuming code closed to that change. IsValid matters for the same reason:
           input chosen at runtime is input you did not write, so the interface has to offer a way to ask before
           committing.
  Expect : Five encodings driven through one interface, each reporting its own name and description. The validity
           checks show the alphabet doing real work - Base58 excludes 0, O, I and l precisely because they are the
           characters a human transcribing a value gets wrong, so a string containing them is rejected rather than
           decoded into something plausible.

  base16-lower     -> '426f647521'  (Base16 / hexadecimal, lower case (default Bodu Base16 form; compatible with Convert.ToHexStringLower).)
  base32-crockford -> '89QP8X91'  (Crockford Base32 (0-9, A-Z minus I/L/O/U; no padding).)
  base58           -> '8VjE9ma'  (Bitcoin/Flickr Base58 (1-9, A-Z minus O/I, a-z minus l).)
  base64-urlsafe   -> 'Qm9kdSE'  (RFC 4648 §5 URL- and filename-safe Base64 (-, _; no padding).)
  z85              -> '?MsJX'  (ZeroMQ Z85 (RFC 32 shell-safe alphabet, 4-byte aligned).)
  (the loop above never names a concrete codec - adding an encoding to the catalogue needs no change here)

  base58.IsValid("Bodu58ok") : True  (expected True - every character is in the Base58 alphabet)
  base58.IsValid("0OIl")     : False  (expected False - these four are exactly the characters Base58 omits, because humans confuse them with O/0 and I/1/l)
```

**APIs demonstrated.** `BinaryEncodings.Get(name)`, `IBinaryEncoding.Encode` / `.Name` /
`.Description` / `.IsValid`.

## Layout

```text
Bodu.Text.Encoding.Samples.EncodingTour/
  Program.cs                        # runs the scenarios in order
  SampleConsole.cs                  # the What / Why / Expect scenario banner
  Scenarios/VariantsTour.cs
  Scenarios/FormattingAndStyles.cs
  Scenarios/ChecksummedSchemes.cs
  Scenarios/GuidConvenience.cs
  Scenarios/EncodingRegistry.cs
```

## Related

- `Bodu.Text.Encoding.Samples.CustomEncoding` — implementing `IBinaryEncoding` yourself,
  with the library's contract-test base proving the implementation.
- Guides: `docs/guides/text-encoding/`.
