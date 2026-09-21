# Bodu.Text.Encoding.Samples.CustomEncoding

Extending the encoding catalogue: a complete custom codec — `Base36Encoding` (digits `0-9`
then `A-Z`, the alphabet of license keys and short URLs) — implementing the library's
`IBinaryEncoding` interface, plus a companion test project that derives the library's own
`BinaryEncodingContractTests<TEncoding>` base to *prove* the implementation honours the
shared contract. Offline and deterministic; no data files.

```bash
dotnet run --project samples/Text.Encoding/Bodu.Text.Encoding.Samples.CustomEncoding
dotnet test samples/Text.Encoding/Bodu.Text.Encoding.Samples.CustomEncoding.Test --settings bvt.runsettings
```

## The implementation — `Base36Encoding`

`Base36Encoding : IBinaryEncoding` treats the payload as one unsigned big-endian integer
(the same model as Base58): no padding, no alignment requirement, and each leading zero byte
is preserved as a leading `'0'` character (Base58's leading-`'1'` rule, for `'0'`). The
implementation deliberately favours clarity — it round-trips through
`System.Numerics.BigInteger` — over the in-place buffer division a production codec would
use; the *contract* it must satisfy is identical either way, which is exactly what the test
project verifies. All nine interface members are implemented: `Encode`/`Decode`,
`TryEncode`/`TryDecode`, `IsValid`, `GetMaxEncodedLength`/`GetMaxDecodedLength`, `Name`, and
`Description`.

## Scenario 1 — EncodeDecode

**Intent.** Exercise the custom codec's own surface: the encode/decode loop, the
leading-zero contract, validation, the Try pattern into caller-owned buffers, and the
`FormatException` thrown for text outside the alphabet — the failure contract the library's
codecs share.

**What it does.** Encodes `Bodu!`, round-trips it, encodes `[00 00 FF]` to show the two
leading `'0'` characters restoring to two zero bytes, checks `IsValid` on good and bad text,
`TryDecode`s into a stack-allocated span, and catches the `FormatException` from decoding
text with a non-alphabet character.

**What to expect.**

```text
--- Base36Encoding - the custom codec through its own surface ---
  What   : Encodes and decodes a payload, checks a value whose leading bytes are zero, validates two strings,
           decodes into a stack-allocated buffer with the Try pattern, and shows Decode throwing on input outside
           the alphabet.
  Why    : Base36 is a big-integer encoding rather than a bit-packing one: the payload is treated as a single number
           and divided repeatedly by 36. That has one consequence worth designing around - a number has no way to
           remember how many leading zeros preceded it, so a naive implementation silently drops them and decodes to
           fewer bytes than it encoded. Base58 solves this by emitting one alphabet character per leading zero byte,
           and this codec adopts the same rule with '0'. The Try pattern matters for the same class of reason: text
           that arrives from outside is not known to be well-formed, and a failed decode should be a return value
           rather than an exception on a hot path.
  Expect : The round trip returns the original bytes, and the zero-prefixed value restores all three bytes rather
           than one - that is the leading-zero rule working. Validation accepts alphabet characters and rejects
           punctuation, TryDecode reports success and the byte count without allocating, and Decode throws for the
           same input TryDecode would have refused.

  payload      : 5 bytes 'Bodu!'
  encoded      : 3N2Y3NS1
  round trip   : True  (expected True - the basic contract every codec owes: decode(encode(x)) == x)
  [00 00 FF]   : '0073' -> 3 bytes restored  (expected 3, not 1 - the two leading '0' characters carry the zero bytes a big-integer encoding would otherwise lose)
  IsValid('9Z'): True, IsValid('a-b'): False  (expected True then False - the hyphen is outside the 36-character alphabet)
  TryDecode    : True, 5 bytes into a stack buffer  (no allocation and no exception - the shape to use for input arriving from outside)
  Decode throws: 'n' is not a Base36 digit (expected 0-9 or A-Z).  (the same input TryDecode would have refused, surfaced as an exception by the throwing overload)
```

**APIs demonstrated.** `IBinaryEncoding.Encode` / `.Decode` / `.IsValid` / `.TryDecode`,
leading-zero preservation, `FormatException` on malformed input.

## Scenario 2 — RegistryComparison

**Intent.** Show the payoff of implementing the interface: the custom codec is a drop-in
peer of the built-in catalogue. A harness written against `IBinaryEncoding` drives
`Base36Encoding` and four registry encodings identically — no special cases.

**What it does.** Builds an `IBinaryEncoding[]` mixing `BinaryEncodings.Get(...)` lookups
with `new Base36Encoding()`, encodes the same 6-byte payload through each, verifies every
round trip, and shows `GetMaxEncodedLength` budgeting a destination buffer for the custom
codec.

**What to expect.** Base36 slotting between Crockford Base32 and Base58 in output length:

```text
--- IBinaryEncoding - the custom codec as a peer of the catalogue ---
  What   : Puts the custom Base36 codec into an array alongside four registry encodings, drives all five through the
           interface to encode and round-trip one payload, and asks the custom codec for its worst-case encoded
           length.
  Why    : This is the reason to implement the library's interface rather than exposing a pair of static methods.
           Anything written against IBinaryEncoding - a config-driven pipeline, a comparison harness, a test suite,
           the registry itself - accepts the custom codec without knowing it exists. GetMaxEncodedLength is part of
           that bargain: a caller that wants to encode into a buffer it owns needs to size it before calling, and it
           can only do that if every codec answers the question the same way.
  Expect : One loop, five codecs, each reporting its own name and round-tripping correctly - the harness never
           branches on which one it holds. Character counts fall as the alphabet grows, and the Base36 row sits
           between Crockford Base32 and Base58 as its alphabet size predicts. GetMaxEncodedLength returns an upper
           bound rather than the exact length, which is what makes it safe to size a buffer with before the data is
           known.

  payload: 6 bytes
  base16-lower     2710ff00429c   (12 chars, round trip True)
  base32-crockford 4W8FY022KG     (10 chars, round trip True)
  base36           F84RTHIU4      (9 chars, round trip True)
  base58           LTKzaPoZ       (8 chars, round trip True)
  base64           JxD/AEKc       (8 chars, round trip True)
  (every row round trips, and the custom codec is reached through the same interface as the built-ins)
  base36.GetMaxEncodedLength(6) = 11 chars (actual: 9)  (an upper bound, not the exact length - it has to be safe for the worst-case payload of that size)
```

**APIs demonstrated.** Interface-polymorphic use of `IBinaryEncoding`,
`BinaryEncodings.Get`, `GetMaxEncodedLength`.

## The contract test — `Bodu.Text.Encoding.Samples.CustomEncoding.Test`

`Base36EncodingContractTests` derives the library test suite's
`BinaryEncodingContractTests<Base36Encoding>` (namespace `Bodu.Text.Encoding.Contracts`) and
supplies only:

- the four adapter members routing `Encode` / `Decode` / `TryEncode` / `TryDecode` to the
  sample codec, and
- the data: six `BinaryEncodingKat` known-answer rows (including the leading-zero and
  boundary vectors) and four `InvalidEncodedTextKat` rejection rows.

The inherited tests then verify encode/decode parity against the vectors, round-trip
integrity, the Try-pattern's too-small-destination behaviour, and rejection of every invalid
input — the same bar the library's own Base45 and friends are held to. This mirrors the
solution's test conventions: the test project references `Bodu.Test` and the
`Bodu.Text.Encoding.Test` project (where the contract base and KAT records live, per the
"colocate with the consumer" rule) and runs in the default BVT tier.

## Layout

```text
Bodu.Text.Encoding.Samples.CustomEncoding/
  Program.cs                       # runs the scenarios in order
  SampleConsole.cs                 # the What / Why / Expect scenario banner
  Base36Encoding.cs                # the IBinaryEncoding implementation
  Scenarios/EncodeDecode.cs
  Scenarios/RegistryComparison.cs
Bodu.Text.Encoding.Samples.CustomEncoding.Test/
  Base36EncodingContractTests.cs   # derives BinaryEncodingContractTests<Base36Encoding>
```

## Related

- `Bodu.Text.Encoding.Samples.EncodingTour` — the built-in catalogue the custom codec
  joins, including the `BinaryEncodings` registry.
- Guides: `docs/guides/text-encoding/`.
