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
Base16   : 426f647521
Base32   : IJXWI5JB
Base58   : 8VjE9ma
Base64   : Qm9kdSE=
...
Base32 Crockford : 89QP8X91
Base32 ZBase32   : ejzse7jb
decode with matching variant: True
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
default        : deadbeef0123
UpperCase      : DEADBEEF0123
IncludePrefix  : 0xdeadbeef0123
InsertSpacing  : de ad be ef 01 23
strict parse rejects   : True
AllowPrefix|IgnoreWs   : 6 bytes recovered -> True
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
Base58Check : 12hq5aKRGagYDkza8o7
  corrupt last char -> Base58Check checksum verification failed.
Bech32      : sample1qq2828nkaqver9k5nfpnq4
  corrupt last char -> Bech32 checksum verification failed.
  intact decode     -> hrp 'sample', 10 bytes, Bech32 (True)
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
Guid.ToString()   : 8f3b2b6e-...-9c47 (36 chars)
Base16            : ... (32 chars)
Base32 Crockford  : ... (26 chars)
Base58            : ... (22 chars)
Base64 UrlSafe    : ... (22 chars)
round trip        : True
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
base16-lower     -> '426f647521'  (Base16 / hexadecimal, lower case ...)
base32-crockford -> '89QP8X91'  (Crockford Base32 ...)
...
base58.IsValid("Bodu58ok") : True
base58.IsValid("0OIl")     : False (alphabet excludes 0, O, I, l)
```

**APIs demonstrated.** `BinaryEncodings.Get(name)`, `IBinaryEncoding.Encode` / `.Name` /
`.Description` / `.IsValid`.

## Layout

```text
Bodu.Text.Encoding.Samples.EncodingTour/
  Program.cs                        # runs the scenarios in order
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
