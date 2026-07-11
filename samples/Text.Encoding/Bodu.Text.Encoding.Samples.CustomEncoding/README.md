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
encoded      : 3N2Y3NS1
round trip   : True
[00 00 FF]   : '0073' -> 3 bytes restored
IsValid('9Z'): True, IsValid('a-b'): False
TryDecode    : True, 5 bytes into a stack buffer
Decode throws: 'n' is not a Base36 digit (expected 0-9 or A-Z).
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
  base16-lower     2710ff00429c   (12 chars, round trip True)
  base32-crockford 4W8FY022KG     (10 chars, round trip True)
  base36           F84RTHIU4      (9 chars, round trip True)
  base58           LTKzaPoZ       (8 chars, round trip True)
  base64           JxD/AEKc       (8 chars, round trip True)
base36.GetMaxEncodedLength(6) = 11 chars (actual: 9)
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
