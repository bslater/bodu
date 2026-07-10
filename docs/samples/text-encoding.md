---
title: Runnable samples
---

# Runnable samples

The repository ships runnable, self-contained sample projects for `Bodu.Text.Encoding` under
[`samples/Text.Encoding/`](https://github.com/bslater/bodu/tree/master/samples/Text.Encoding).
Both samples are **pure computation over fixed payloads** — offline, deterministic, no data
files — and are members of `bodu.slnx`, built and executed by CI; the companion contract-test
project runs with the test suites. Each README documents every scenario individually: its
intent, what the code does, the output to expect, and the APIs demonstrated.

Run either sample from the repository root:

```bash
dotnet run --project samples/Text.Encoding/<SampleName>
```

## The samples

### Bodu.Text.Encoding.Samples.EncodingTour

The catalogue, end to end: one payload through every base family (Base16/32/45/58/62/64/85)
and one family through its variants (<xref:Bodu.Text.Encoding.Base32Variant>,
<xref:Bodu.Text.Encoding.Base85Variant>) — the alphabet is an enum argument, not a different
API; the option enums that bracket every codec —
<xref:Bodu.Text.Encoding.BaseFormattingOptions> shaping produced text (case, `0x` prefixes,
spacing, padding) and <xref:Bodu.Text.Encoding.BaseFormatStyles> declaring parse tolerance
(prefixes, whitespace, missing padding); the checksummed schemes for identifiers humans
re-type — <xref:Bodu.Text.Encoding.Base58Check> and <xref:Bodu.Text.Encoding.Bech32> — each
shown detecting a single corrupted character; the `Guid` convenience overloads (36-char Guid
string down to 22 chars in Base58); and the name-addressable
<xref:Bodu.Text.Encoding.BinaryEncodings> registry driving codecs through
<xref:Bodu.Text.Encoding.IBinaryEncoding> when the encoding is chosen at runtime. *Package:
`Bodu.Text.Encoding`.*

### Bodu.Text.Encoding.Samples.CustomEncoding (+ .Test)

Extending the catalogue: a complete Base36 codec (digits `0-9` then `A-Z` — license keys,
short URLs) implementing <xref:Bodu.Text.Encoding.IBinaryEncoding> with the big-endian
integer model and leading zero bytes preserved as `'0'`; exercised through its own surface
(round trips, validation, the Try pattern, `FormatException` on malformed input) and then
side by side with registry codecs through the shared interface — a drop-in peer. The
companion test project derives the library's `BinaryEncodingContractTests<Base36Encoding>`
(from the `Bodu.Text.Encoding.Test` contracts) with known-answer and invalid-input KAT rows,
inheriting the encode/decode/round-trip/Try contract suite the built-in codecs pass.
*Package: `Bodu.Text.Encoding`.*

## Guarded documentation

The encoding guides under [`docs/guides/text-encoding/`](../guides/text-encoding/index.md)
carry compile-guarded snippets: examples marked with a `<!-- compile -->` sentinel are
compiled against the current public API by `DocumentationSnippetCompileTests` in the
library's test project (Regression tier).

## Related

- [Text.Encoding guides](../guides/text-encoding/index.md) — per-base pages, the helpers, and
  the `IBinaryEncoding` interface documentation.
- [Bencode samples](bencode.md) — the torrent info-hash scenario consumes `Base16` from this
  package.
