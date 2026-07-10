# Text.Encoding Samples

Console applications demonstrating the `Bodu.Text.Encoding` binary-encoding catalogue. Each
sample is a standalone project; run one with:

```bash
dotnet run --project samples/Text.Encoding/<SampleName>
```

Every sample is pure computation over fixed payloads — offline and deterministic, no data
files. The `CustomEncoding.Test` project runs with the library test suites in CI.

## Sample → pattern → package matrix

| Sample | Demonstrates | Packages |
|---|---|---|
| `Bodu.Text.Encoding.Samples.EncodingTour` | One payload through every base family and the Base32/Base85 variants, `BaseFormattingOptions` (write) vs `BaseFormatStyles` (read), checksummed Base58Check and Bech32 corruption detection, the `Guid` convenience overloads, and the name-addressable `BinaryEncodings` registry over `IBinaryEncoding` | `Bodu.Text.Encoding` |
| `Bodu.Text.Encoding.Samples.CustomEncoding` (+ `.Test`) | A complete custom Base36 `IBinaryEncoding` (big-endian integer model, leading zero bytes preserved as `'0'`), exercised through its own surface and side by side with registry codecs; the test project derives the library's `BinaryEncodingContractTests<Base36Encoding>` with KAT rows | `Bodu.Text.Encoding` |
