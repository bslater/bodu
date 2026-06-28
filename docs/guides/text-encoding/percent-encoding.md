---
title: Using percent-encoding (URIs and forms)
---

# Using percent-encoding (URIs and forms)

`PercentEncoding` implements the URI percent-encoded octet form of **RFC 3986 §2.1**, with the WHATWG URL Standard
`application/x-www-form-urlencoded` rules available as a mode. Each byte that is not allowed unescaped in the selected
component becomes a `%HH` triple with uppercase hexadecimal digits.

```
input         : a/b?c=d                        (7 ASCII characters)
UriComponent  : a%2Fb%3Fc%3Dd                  ('/', '?', '=' are reserved → escaped)
```

> [!NOTE]
> `PercentEncoding` is **not a URL parser**. It does not resolve relative URLs, normalise hosts, paths, dot-segments,
> or scheme casing, implement IDNA / IRI processing, accept the obsolete `%uXXXX` escape, or depend on `System.Web`.
> It encodes and decodes a value according to an explicitly selected component rule.

## Quick reference

```csharp
using Bodu.Text.Encoding;

// URI component (default) — only unreserved characters pass through.
string component = PercentEncoding.EncodeString("a/b?c=d");                 // a%2Fb%3Fc%3Dd

// Form field — space becomes '+'.
string field = PercentEncoding.EncodeString("a b+c", mode: PercentEncodingMode.FormUrlEncoded); // a+b%2Bc

// Round-trip.
string value = PercentEncoding.DecodeString("a%2Fb");                       // a/b
```

Like Quoted-Printable, percent-encoding is content-dependent and mode-driven, so it is a **static type, not an
[`IBinaryEncoding`](binary-encodings-interface.md)**.

## Component modes

| Mode | Passes through unescaped | Use for |
|---|---|---|
| `UriComponent` (default) | unreserved `ALPHA DIGIT - . _ ~` | A standalone value embedded in any URI component |
| `PathSegment` | unreserved + sub-delims + `:` `@` (encodes `/` `?` `#`) | One path segment — encode each segment, then join with `/` |
| `Query` | unreserved + sub-delims + `:` `@` `/` `?` (encodes `#`) | A whole query component |
| `FormUrlEncoded` | ASCII alphanumeric + `*` `-` `.` `_` | HTML form / query name-value data |

```csharp
PercentEncoding.Encode("/"u8, PercentEncodingMode.UriComponent);   // %2F
PercentEncoding.Encode("/"u8, PercentEncodingMode.Query);          // /   (slash allowed in a query)
PercentEncoding.Encode("#"u8, PercentEncodingMode.Query);          // %23 (fragment marker always encoded)
```

### Form mode specifics

`FormUrlEncoded` follows the WHATWG serializer rather than RFC 3986:

| Octet | Output |
|---|---|
| Space `0x20` | `+` |
| Plus `+` | `%2B` |
| Percent `%` | `%25` |
| `~` | `%7E` (not in the form pass-through set) |
| ASCII alphanumeric, `*`, `-`, `.`, `_` | Literal |

`+` is treated as a space **only** in `FormUrlEncoded` mode — in every other mode it is a literal plus byte.

## Hex casing

Encoding always emits **uppercase** hex (`%2F`, `%E2%80%BD`), the form RFC 3986 recommends for producers. Decoding
accepts **both** cases, which RFC 3986 defines as equivalent:

```csharp
PercentEncoding.Decode("%2F");   // { 0x2F }
PercentEncoding.Decode("%2f");   // { 0x2F }
```

## Decoding: strict vs relaxed

By default the decoder rejects a malformed percent triple (`%`, `%A`, `%GG`, `%u1234`) and any non-ASCII character in
the byte-oriented surface. The relaxation is opt-in:

```csharp
PercentEncoding.Decode("%GG");                                                   // FormatException
PercentEncoding.Decode("%GG", options: PercentDecodingOptions.AllowInvalidPercentLiterals); // { '%', 'G', 'G' }
```

`AllowInvalidPercentLiterals` copies an invalid percent sequence literally as its ASCII bytes (WHATWG-style leniency)
and never changes the decoding of a valid escape.

## String helpers and text encodings

`EncodeString` / `DecodeString` bridge a .NET `string` through a text encoding (UTF-8 by default):

```csharp
PercentEncoding.EncodeString("‽");                       // %E2%80%BD  (UTF-8 octets)
PercentEncoding.DecodeString("%E2%80%BD");               // ‽

// Supply any System.Text.Encoding; its decoder fallback governs invalid input.
PercentEncoding.DecodeString("%FF");                     // "�" (UTF-8 replacement fallback)
```

The byte-oriented `Decode` / `TryDecode` reject non-ASCII source characters — Unicode belongs in `DecodeString`.

## Validation and sizing

```csharp
PercentEncoding.IsValid("a%2Fb");                            // true
PercentEncoding.IsValid("%GG");                              // false
PercentEncoding.IsValid("a b");                              // false — literal space is not canonical
PercentEncoding.IsValid("a/b?c", PercentEncodingMode.Query); // true — '/' and '?' are allowed in a query

PercentEncoding.GetEncodedLength(value, mode);           // exact encoded length
PercentEncoding.GetMaxEncodedLength(value.Length);       // worst case = length * 3
PercentEncoding.TryGetDecodedLength(text, out int n, mode); // exact decoded length, false if malformed
```

`IsValid` checks **canonical** conformance for the mode: a literal character the mode would percent-encode (a space, or
`#` in a URI component) makes it return `false`, while a percent-escaped octet such as `%2F` is always accepted.
`Decode` is more lenient — it still recovers a literal reserved character — so `IsValid(x) == true` implies `Decode(x)`
succeeds, but not the reverse. `TryGetDecodedLength` mirrors `Decode`, so it can size a buffer for any decodable input.

## Span path

```csharp
char[] buffer = new char[PercentEncoding.GetMaxEncodedLength(value.Length)];
bool ok = PercentEncoding.TryEncode(value, buffer, out int written, PercentEncodingMode.UriComponent);
```

`TryEncode` / `TryDecode` never throw — they return `false` and write `0` for an undefined mode, malformed input, or an
undersized destination.

## Where to go next

- **[Quoted-Printable guide](quoted-printable.md)** — the MIME body `=HH` escape encoding.
- **[Base64 guide](base64.md)** — the URL-safe variant when you need compact transport rather than readable URLs.
- **[Text & Serialization guides](../topics/text-and-serialization.md)** — every guide in this topic.
