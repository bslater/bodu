---
title: Formatting and parsing Fraction<T>
---

# Formatting and parsing `Fraction<T>`

Exact rational values only round-trip when their textual form preserves every bit of the canonical representation. `Fraction<T>` ships three first-class text forms — the improper ratio `7/3`, the mixed number `2 1/3`, and the single-codepoint Unicode glyph `2⅓` — plus a percentage form. Every output form is also an accepted input form, so any `ToString` result feeds back through `Parse` to the same value.

This guide covers what each specifier renders, what the parser accepts, and how culture and span surfaces interact with both. For the rest of the type, start with [Working with `Fraction<T>`](fraction.md).

## Format specifiers at a glance

| Specifier | Output | Example (`7/3`) |
|---|---|---|
| `G` (default) | improper ratio | `7/3` |
| `M` | mixed number | `2 1/3` |
| `U` | Unicode vulgar fraction with mixed-number fallback | `2⅓` |
| `P` | percentage | `233 1/3%` |

Specifiers are case-insensitive — `Format` uppercases the first character — and the format string `null`, `""`, or `"G"` all select the general form. Any other specifier throws <xref:System.FormatException>.

## Improper-ratio form (default)

`ToString()` and `ToString("G")` render the canonical pair as `numerator/denominator`. When the canonical denominator is one, the slash and denominator are omitted so whole numbers print as a bare integer:

```csharp
Fraction<int>.Create(3, 4).ToString();    // "3/4"
Fraction<int>.Create(-7, 4).ToString();   // "-7/4"  — sign rides on the numerator
Fraction<int>.Create(6, 2).ToString();    // "3"     — canonical denominator is one
Fraction<int>.Zero.ToString();            // "0"
```

The general form is the round-trip wire shape: `JsonSerializer.Serialize` (see below) and `<xref:Bodu.Numerics.Fraction`1>.Parse` both treat `"n/d"` as the lossless text form.

## Mixed-number form

`ToString("M")` separates the whole part from the proper remainder with a single space. The whole part carries the sign; the fractional part is always written with a non-negative numerator. Whole-number and proper-fraction values short-circuit to their bare forms:

```csharp
Fraction<int>.Create(7, 4).ToString("M");    // "1 3/4"
Fraction<int>.Create(-7, 4).ToString("M");   // "-1 3/4"  — sign on the whole part
Fraction<int>.Create(11, 4).ToString("M");   // "2 3/4"
Fraction<int>.Create(3, 4).ToString("M");    // "3/4"     — proper fraction: no whole part
Fraction<int>.Create(4, 1).ToString("M");    // "4"       — whole number: no fractional part
Fraction<int>.Zero.ToString("M");            // "0"
```

The convenience methods <xref:Bodu.Numerics.Fraction`1>.`ToMixedString(provider)` and `ToMixedNumberString(provider)` are aliases for `ToString("M", provider)`.

## Unicode vulgar-fraction form

`ToString("U")` emits a single-codepoint glyph when one exists for the proper-fraction part. The 18 glyphs supported are the Unicode "Number Forms" vulgar fractions:

| Glyph | Value | Glyph | Value | Glyph | Value |
|---|---|---|---|---|---|
| `½` | 1/2 | `⅖` | 2/5 | `⅛` | 1/8 |
| `⅓` | 1/3 | `⅗` | 3/5 | `⅜` | 3/8 |
| `⅔` | 2/3 | `⅘` | 4/5 | `⅝` | 5/8 |
| `¼` | 1/4 | `⅙` | 1/6 | `⅞` | 7/8 |
| `¾` | 3/4 | `⅚` | 5/6 | `⅑` | 1/9 |
| `⅕` | 1/5 | `⅐` | 1/7 | `⅒` | 1/10 |

When the canonical proper-fraction remainder matches one of these pairs, the result is `[sign][whole part][glyph]` with the whole part suppressed when zero. When no glyph applies, the formatter falls back to the mixed-number form:

```csharp
Fraction<int>.Create(1, 2).ToString("U");    // "½"
Fraction<int>.Create(3, 4).ToString("U");    // "¾"
Fraction<int>.Create(7, 4).ToString("U");    // "1¾"     — whole part + glyph, no separator
Fraction<int>.Create(-3, 4).ToString("U");   // "-¾"
Fraction<int>.Create(5, 2).ToString("U");    // "2½"
Fraction<int>.Create(3, 1).ToString("U");    // "3"      — whole number
Fraction<int>.Create(5, 9).ToString("U");    // "5/9"    — no 5/9 glyph: falls back to mixed
```

The convenience method <xref:Bodu.Numerics.Fraction`1>.`ToUnicodeString(provider)` is an alias for `ToString("U", provider)`.

## Parsing

`Fraction<T>.Parse` and `TryParse` accept every output form the type produces, plus a few additional ergonomic shapes. The grammar, in order of recognition:

| Input shape | Example | Parses as |
|---|---|---|
| Whole integer | `"3"`, `"-5"`, `"+12"` | `3/1`, `-5/1`, `12/1` |
| Ratio | `"3/4"`, `"-7/2"` | `3/4`, `-7/2` |
| Mixed number (space or tab separated) | `"2 1/3"`, `"-1 3/4"` | `7/3`, `-7/4` |
| Vulgar-fraction glyph | `"½"`, `"⅗"` | `1/2`, `3/5` |
| Whole + glyph | `"2⅜"`, `"-1¾"` | `19/8`, `-7/4` |
| Percentage | `"75%"`, `"100/3%"` | `3/4`, `1/3` |

Parsing is lenient about whitespace — leading and trailing whitespace is trimmed, and the whole / fractional parts of a mixed number are trimmed individually. A `+` or `-` sign at the start applies to the entire result; for mixed numbers the sign therefore rides on the whole + fraction sum, not the whole part alone. The trailing `%` divides the parsed denominator by 100 in lowest terms.

```csharp
Fraction<int>.Parse("3/4");          // 3/4
Fraction<int>.Parse("  2 1/3  ");    // 7/3       — whitespace trimmed
Fraction<int>.Parse("-2 1/3");       // -7/3      — sign covers whole + fraction
Fraction<int>.Parse("⅗");            // 3/5
Fraction<int>.Parse("2⅜");           // 19/8
Fraction<int>.Parse("75%");          // 3/4
Fraction<int>.TryParse("nope", out var _);   // false
```

The numeric components are read with `NumberStyles.None`, so scientific notation and group separators are rejected; the parser is intentionally strict about the shape so the wire format remains unambiguous. A `0` denominator is rejected (`TryParse` returns `false`; `Parse` throws <xref:System.FormatException>), as is any input whose canonical form does not fit in the backing type `T` — overflow is reported through `false` from `TryParse` and through <xref:System.FormatException> from `Parse`.

`Fraction<T>` implements <xref:System.IParsable`1> and <xref:System.ISpanParsable`1>, so the same call sites work for `string` and `ReadOnlySpan<char>` inputs.

## Culture handling

`Fraction<T>` text is digit-slash-digit by construction, so most cultures behave identically — there is no decimal separator, group separator, or percent sign placement to vary. The `IFormatProvider` argument is forwarded to `BigInteger.ToString(provider)` and `BigInteger.TryParse(text, NumberStyles.None, provider, ...)` for the numeric components, so culture-specific digit shapes are respected (e.g. Arabic-Indic digits when the culture's `NumberFormatInfo` calls for them), but the structural characters — `/`, the mixed-number space, glyph codepoints, and the trailing `%` — are invariant.

The JSON converter passes <xref:System.Globalization.CultureInfo>.`InvariantCulture` to both `ToString` and `Parse` so the wire form remains stable regardless of the ambient culture. For application-level formatting where you want the structural form to be the same on every machine, prefer passing `CultureInfo.InvariantCulture` explicitly:

```csharp
var text = value.ToString("M", CultureInfo.InvariantCulture);
var back = Fraction<int>.Parse(text, CultureInfo.InvariantCulture);
```

`Parse(string)` without a provider is equivalent to `Parse(string, null)`, which delegates to `BigInteger.TryParse` with a `null` provider — that uses the current culture, matching the BCL convention.

## Span and UTF-8 surfaces

`Fraction<T>` implements the span and UTF-8 formatting / parsing interfaces so it slots into low-allocation pipelines:

- <xref:System.ISpanFormattable>.`TryFormat(Span<char>, out int, ReadOnlySpan<char>, IFormatProvider?)` — writes the formatted text into a char buffer; returns `false` when the destination is too small.
- <xref:System.IUtf8SpanFormattable>.`TryFormat(Span<byte>, out int, ReadOnlySpan<char>, IFormatProvider?)` — writes the same text UTF-8 encoded into a byte buffer.
- <xref:System.ISpanParsable`1>.`TryParse(ReadOnlySpan<char>, IFormatProvider?, out Fraction<T>)` — parses without allocating a `string`.
- <xref:System.IUtf8SpanParsable`1>.`Parse(ReadOnlySpan<byte>, IFormatProvider?)` and `TryParse(...)` — accept UTF-8 input.

```csharp
Span<char> buffer = stackalloc char[16];
if (value.TryFormat(buffer, out int written, "M", CultureInfo.InvariantCulture))
{
    ReadOnlySpan<char> text = buffer[..written];
    // text is "1 3/4" for value 7/4
}

Fraction<int> parsed = Fraction<int>.Parse("3/4"u8, null);
```

## Round-tripping JSON

`Fraction<T>` carries `[JsonConverter(typeof(FractionJsonConverterFactory))]`, so `System.Text.Json.JsonSerializer` picks up the converter automatically. The wire shape is the general form rendered with <xref:System.Globalization.CultureInfo>.`InvariantCulture` — a single JSON string token, `"numerator/denominator"`, or a bare integer string for whole values:

```csharp
using System.Text.Json;

string json = JsonSerializer.Serialize(Fraction<int>.Create(3, 4));
// "3/4"

Fraction<int> roundTrip = JsonSerializer.Deserialize<Fraction<int>>(json);
```

The converter reads via `Fraction<T>.Parse(text, CultureInfo.InvariantCulture)`; non-string tokens, null strings, and unparseable text raise <xref:System.Text.Json.JsonException>. See the [Working with `Fraction<T>`](fraction.md) guide for the equivalent XML helpers `ToXml()` / `FromXml(string)`.

## See also

- [Working with `Fraction<T>`](fraction.md) — construction, arithmetic, continued fractions, approximation.
- [Bodu.Numerics core concepts](../../docs/numerics/concepts.md) — canonical form, mixed-number, Unicode vulgar fraction.
- <xref:Bodu.Numerics.Fraction`1> — API reference.
- <xref:Bodu.Numerics.Serialization.FractionJsonConverter`1> — JSON converter reference.
- **[Numerics & Financial guides](../topics/numerics-and-financial.md)** — every guide in this topic, across Bodu.Numerics and Bodu.Financial.
