---
title: Using Base62
---

# Using Base62

`Base62` encodes binary data with the **GMP-style** alphabet `0-9 A-Z a-z` (digits first, then upper-case, then
lower-case letters). Like [Base58](base58.md), its radix is **not a power of two**, so it treats the input as a
big-integer and repeatedly divides by 62 to extract digits. Unlike Base58, it keeps the four visually ambiguous
characters Base58 drops (`0`, `O`, `I`, `l`) - Base62 optimises for **density and URL-safety**, not
hand-transcription.

Base62 is the natural choice for short URLs, compact record identifiers, and slugs: every character is a letter or
digit, so the output is URL-safe and shell-safe without any escaping, and the 62-character radix is more compact than
Base58 while staying free of the `+`, `/`, and `=` that make Base64 awkward in a path segment.

```csharp
using Bodu.Text.Encoding;

byte[] data = RandomNumberGenerator.GetBytes(16);

string id = Base62.Encode(data);   // e.g. a ~22-character URL-safe identifier
byte[] back = Base62.Decode(id);   // round-trips exactly
```

## Alphabet ordering

The alphabet is `0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz` - digit `0` is value 0, `A` is 10,
`a` is 36. This is the **GMP / base-conversion** ordering, the most widely used Base62 convention. It is *not* the same
as Base64's alphabet, and the two are not interchangeable.

## Leading zeros

Because Base62 uses big-integer arithmetic, leading zero bytes would normally vanish. The implementation preserves
them as leading `0` characters (where `0` is `alphabet[0]`), exactly as Base58 preserves leading `1` characters:

| Input bytes | Encoded |
|---|---|
| `0x00` | `0` |
| `0x00 0x00` | `00` |
| `0x00 0x01` | `01` |

```csharp
byte[] withZeros = { 0x00, 0x00, 0xDE, 0xAD };
string encoded = Base62.Encode(withZeros);
byte[] back = Base62.Decode(encoded);   // back.SequenceEqual(withZeros)
```

This makes the round trip exact for any byte sequence, including one with a run of leading zeros.

## Lenient parsing

| Flag | Effect |
|---|---|
| `BaseFormatStyles.IgnoreWhitespace` | Strip ASCII space / tab / CR / LF anywhere |
| `BaseFormatStyles.AllowPrefix` | No-op for Base62 |
| `BaseFormatStyles.AllowMissingPadding` | No-op for Base62 (no padding character) |

```csharp
byte[] payload = Base62.Decode("  3p Kq9  ", BaseFormatStyles.IgnoreWhitespace);
```

The decoder otherwise rejects any character outside the 62-symbol alphabet with a `FormatException`; `TryDecode`
returns `false` instead.

## Span path and sizing

```csharp
int maxChars = Base62.GetMaxEncodedLength(payload.Length);   // upper bound (data-dependent)
char[] buffer = new char[maxChars];

int written = Base62.Encode(payload, buffer);
ReadOnlySpan<char> result = buffer.AsSpan(0, written);
```

Exact length is data-dependent (it varies with the leading-zero count and the magnitude of the non-zero portion), so
the library exposes `GetMaxEncodedLength` / `GetMaxDecodedLength` as upper bounds. Base62 uses big-integer arithmetic
and is therefore **not streamable**: each call needs the entire input.

## UTF-8, buffer writers, and GUIDs

Base62 text is ASCII, so the UTF-8 overloads read and write it as one byte per character, for network and file
pipelines that work in bytes. `DecodeFromUtf8` follows the `OperationStatus` convention, but because Base62 is not
streamable it decodes the whole span as one input and never returns `NeedMoreData`. The buffer-writer overloads
encode straight into an `IBufferWriter<char>` or `IBufferWriter<byte>`, and a `Guid` encodes from the bytes
`Guid.TryWriteBytes` writes.

<!-- compile -->
```csharp
byte[] payload = { 0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x21, 0x21 };   // "Hello!!"

byte[] utf8 = Base62.EncodeToUtf8(payload);                      // the ASCII bytes of Base62.Encode(payload)

byte[] decoded = new byte[Base62.GetMaxDecodedLength(utf8.Length)];
System.Buffers.OperationStatus status = Base62.DecodeFromUtf8(utf8, decoded, out int consumed, out int written);

var writer = new System.Buffers.ArrayBufferWriter<byte>();
_ = Base62.EncodeToUtf8(payload, writer);                        // into a pipeline's buffer writer

string id = Base62.Encode(Guid.NewGuid());                       // about 22 characters
Guid back = Base62.DecodeGuid(id);
```

## Validation

```csharp
Base62.IsValid("3pKq9");      // true
Base62.IsValid("3p+q9");      // false - '+' is not in the alphabet
Base62.IsBase62Digit('Z');    // true
Base62.IsBase62Digit('+');    // false
```

## Base58 or Base62?

| Need | Pick |
|---|---|
| Hand-transcribed identifiers (no `0`/`O`/`I`/`l` ambiguity) | [Base58](base58.md) |
| Blockchain / Bitcoin / IPFS interop | [Base58](base58.md) |
| Densest URL-safe identifier, machine-to-machine | **Base62** |
| Built-in checksum on addresses / keys | [Base58Check](base58.md#base58check---checksum-protected-payloads) |

## Where to go next

- **[Base58 guide](base58.md)** - the ambiguity-free sibling, plus `Base58Check`.
- **[Base64 guide](base64.md)** - when interop with existing Base64 tooling matters more than path-safety.
- **[`IBinaryEncoding` interface](binary-encodings-interface.md)** - Base62 is registered as `BinaryEncodings.Base62` for runtime selection.
- **[Text & Serialization guides](../topics/text-and-serialization.md)** - every guide in this topic, across Bodu.Text.Encoding, Bodu.Text.Formats, and the Bencode / TOML serializers.
