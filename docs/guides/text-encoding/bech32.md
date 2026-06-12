---
title: Using Bech32 and Bech32m
---

# Using Bech32 and Bech32m

`Bech32` implements the checksummed base-32 format defined by [BIP 173](https://github.com/bitcoin/bips/blob/master/bip-0173.mediawiki)
(Bech32) and [BIP 350](https://github.com/bitcoin/bips/blob/master/bip-0350.mediawiki) (Bech32m). It is best known
as the encoding of Bitcoin SegWit addresses (`bc1…`) and Lightning BOLT11 invoices, but it is a general-purpose
encoding for any payload that benefits from a human-readable prefix and strong, position-aware error detection.

Unlike the other encodings in this library, Bech32 is **not** a flat binary-to-text transform. An encoded string has
four parts:

```
   bc 1 qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4
   │  │ │                                  │
   │  │ │                                  └─ 6-symbol checksum
   │  │ └─ data part (5-bit groups, drawn from "qpzry9x8gf2tvdw0s3jn54khce6mua7l")
   │  └─ separator '1'
   └─ human-readable part (HRP)
```

Because the HRP and checksum are integral to the string, `Bech32` is modelled on [`Base58Check`](base58.md#base58check--checksum-protected-payloads)
rather than the [`IBinaryEncoding`](binary-encodings-interface.md) family — it sits outside the runtime registry.

## 5-bit groups vs. 8-bit bytes

The core methods operate on **5-bit data groups** (each value `0`–`31`). Two convenience pairs bridge to ordinary
bytes, and `ConvertBits` does it by hand:

| Method | Data form |
|---|---|
| `Encode(hrp, data, scheme)` / `Decode(...)` | 5-bit groups (values 0–31) |
| `EncodeFromBytes(hrp, data, scheme)` / `DecodeToBytes(...)` | 8-bit bytes (repacked with `ConvertBits` internally) |
| `ConvertBits(data, fromBits, toBits, pad)` | manual bit-width conversion |

```csharp
using Bodu.Text.Encoding;

// Round-trip arbitrary bytes through the byte-oriented pair.
byte[] payload = { 0xDE, 0xAD, 0xBE, 0xEF };
string encoded = Bech32.EncodeFromBytes("data", payload, Bech32Encoding.Bech32m);

Bech32.DecodeToBytes(encoded, out string hrp, out byte[] back, out Bech32Encoding scheme);
// hrp == "data"; back.SequenceEqual(payload); scheme == Bech32Encoding.Bech32m
```

## Bech32 vs. Bech32m

The two schemes differ only in the checksum constant, which makes a Bech32m string fail a Bech32 checksum and vice
versa. <xref:Bodu.Text.Encoding.Bech32Encoding> selects the scheme on encode, and the decoder **reports which scheme
validated** the checksum through an out parameter:

| Scheme | BIP | Use |
|---|---|---|
| `Bech32Encoding.Bech32` (default) | BIP 173 | SegWit v0 (`bc1q…`), Lightning invoices |
| `Bech32Encoding.Bech32m` | BIP 350 | SegWit v1+ / Taproot (`bc1p…`) |

```csharp
Bech32.Decode("bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4",
    out string hrp, out byte[] data, out Bech32Encoding scheme);
// scheme == Bech32Encoding.Bech32
```

## Worked example — a SegWit v0 address

A SegWit address is not a plain byte payload: the data part is a one-symbol **witness version** followed by the
witness program repacked from 8 bits to 5. Witness v0 uses Bech32; v1+ uses Bech32m.

```csharp
using Bodu.Text.Encoding;

byte[] program = Base16.Decode("751e76e8199196d454941c45d1b3a323f1433bd6"); // 20-byte HASH160

byte[] groups = Bech32.ConvertBits(program, 8, 5, pad: true)!;   // program → 5-bit groups
byte[] data = new byte[1 + groups.Length];
data[0] = 0;                                                     // witness version 0
groups.CopyTo(data, 1);

string address = Bech32.Encode("bc", data, Bech32Encoding.Bech32);
// "bc1qw508d6qejxtdg4y5r3zarvary0c5xw7kv8f3t4"
```

## Case handling

Encoded output is always **lower case** (the canonical form). The decoder accepts an all-lower-case or all-upper-case
string and rejects mixed case per BIP 173; the returned HRP is normalised to lower case.

```csharp
Bech32.IsValid("BC1QW508D6QEJXTDG4Y5R3ZARVARY0C5XW7KV8F3T4");   // true  (all upper)
Bech32.IsValid("bc1qw508d6qejxtdg4Y5r3zarvary0c5xw7kv8f3t4");   // false (mixed case)
```

## The 90-character limit

BIP 173 caps an address at **90 characters**. The decoder enforces this limit; the encoder does **not**, so it can
produce the longer strings non-address schemes need — Lightning BOLT11 invoices routinely exceed 90 characters.

## HRP rules

The human-readable part must be **non-empty** and contain only US-ASCII characters in the range 33–126. An empty or
out-of-range HRP throws `ArgumentException` from the encoder; `TryEncode` returns `false`.

## Non-throwing forms

```csharp
if (Bech32.TryEncode("bc", data, Bech32Encoding.Bech32, out string? encoded))
{
    // encoded is non-null
}

if (Bech32.TryDecodeToBytes(input, out string? hrp, out byte[]? bytes, out Bech32Encoding scheme))
{
    // hrp / bytes are non-null; scheme reports Bech32 vs Bech32m
}
```

`Decode` / `DecodeToBytes` throw `FormatException` on a bad checksum, mixed case, missing separator, over-length
input, or an out-of-alphabet character; the `Try*` forms return `false` instead. `Bech32.IsValid` is a shorthand for
"does this decode without error".

## Sizing

```csharp
// hrpLength + 1 separator + dataLength + 6 checksum symbols
int length = Bech32.GetEncodedLength(hrpLength: 2, dataLength: 33);
```

## Where to go next

- **[Base58 guide](base58.md)** — the legacy Bitcoin address encoding and `Base58Check`.
- **[Base32 guide](base32.md)** — the plain 5-bit encoding Bech32's data part is built on.
- **[`IBinaryEncoding` interface](binary-encodings-interface.md)** — the runtime registry for the flat-byte encodings (Bech32 stays outside it).
- **[Text & Serialization guides](../topics/text-and-serialization.md)** — every guide in this topic, across Bodu.Text.Encoding, Bodu.Text.Formats, and the Bencode / TOML serializers.
