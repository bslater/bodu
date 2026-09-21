---
title: String extensions
---

# String extensions

<xref:Bodu.Extensions.StringExtensions> is a static class of 77 extension methods on `string` that cover the small, recurring text chores the BCL leaves to every project: pulling a substring out by marker, wrapping and unwrapping, normalizing whitespace and line endings, ordinal predicates, null/empty coalescing, affix management, character filtering, identifier casing, slugs, safe file names, and truncation. Every method is pure — it returns a new string and never mutates its receiver.

The surface is organized below by task. Each table gives the one-line contract and the behaviour on `null` or empty input; every example in this guide has been run against the library, and the values in the comments are the real outputs.

> [!NOTE]
> **Null and empty conventions.** Methods whose receiver is declared `string` (not `string?`) throw <xref:System.ArgumentNullException> when called on `null`; only the eight coalescing helpers in [Pattern 5](#pattern-5--null-empty-and-whitespace-coalescing) accept a `null` receiver. An empty receiver is always valid and yields the natural empty result (an empty string, an empty sequence, `false`, or — for the substring family — `null` because the marker cannot be found). Every string comparison defaults to <xref:System.StringComparison.Ordinal>; the methods that take a `StringComparison` parameter accept any member.

## Pattern 1 — substring by marker

`After` / `AfterLast` / `Before` / `BeforeLast` / `Between` return the text on one side of a marker, or `null` when the marker is absent — which makes them safe to chain with `??`.

```csharp
using Bodu.Extensions;

string url = "https://example.com/docs/guide.md?lang=en#intro";

string? scheme = url.Before("://");              // "https"
string? path   = url.After("example.com");       // "/docs/guide.md?lang=en#intro"
string? file   = url.Between("/docs/", "?");     // "guide.md"
string? ext    = url.AfterLast(".");             // "md?lang=en#intro"
string? stem   = url.BeforeLast("?");            // "https://example.com/docs/guide.md"
string? none   = url.After("ftp://");            // null — marker absent
```

| Method | Returns | Marker absent |
|---|---|---|
| `After(marker, comparison)` | Text following the **first** occurrence of `marker`. | `null` |
| `AfterLast(marker, comparison)` | Text following the **last** occurrence. | `null` |
| `Before(marker, comparison)` | Text preceding the **first** occurrence. | `null` |
| `BeforeLast(marker, comparison)` | Text preceding the **last** occurrence. | `null` |
| `Between(start, end, comparison)` | Text between the first `start` and the next `end` after it. | `null` (either marker) |

The markers must not be `null` (`ArgumentNullException`). An empty marker matches at position 0, so `After("")` returns the whole string and `Before("")` returns an empty string.

## Pattern 2 — wrapping and unwrapping

```csharp
using Bodu.Extensions;

string name = "alpha";

string quoted   = name.Quote();              // "alpha"   (straight double quotes)
string single   = name.SingleQuote();        // 'alpha'
string parens   = name.Parenthesize();       // (alpha)
string brackets = name.Bracket();            // [alpha]
string braces   = name.Brace();              // {alpha}
string custom   = name.Wrap("<<", ">>");     // <<alpha>>

string inner    = "[alpha]".Unwrap("[", "]");   // alpha
string partial  = "[alpha".Unwrap("[", "]");    // [alpha — both ends must be present
```

| Method | Behaviour | Empty receiver |
|---|---|---|
| `Quote()`, `SingleQuote()`, `Parenthesize()`, `Bracket()`, `Brace()` | Wraps in the named pair. | Returns just the pair (`""`, `()`, …). |
| `Wrap(prefix, suffix)` | Prepends `prefix` and appends `suffix`. | Returns `prefix + suffix`. |
| `Unwrap(prefix, suffix, comparison)` | Removes both only when **both** are present at their ends; otherwise returns the input unchanged. | Unchanged. |

## Pattern 3 — whitespace, line endings, and lines

The whitespace family works on the whole string; the line family treats the string as a sequence of lines delimited by CRLF, CR, or LF.

```csharp
using Bodu.Extensions;

string raw = "  line one\r\n\r\n   line   two\rline three\n";

string normalized = raw.NormalizeLineEndings();   // every CRLF / CR / LF becomes "\n"
string collapsed  = raw.CollapseWhitespace();     // " line one line two line three " — runs become one space
string compact    = raw.RemoveWhitespace();       // "lineonelinetwolinethree"

foreach (string line in raw.SplitLines(removeEmptyLines: true))
    Console.WriteLine($"[{line.Trim()}]");        // [line one] / [line   two] / [line three]
```

```csharp
using Bodu.Extensions;

string block = "select *\nfrom users\nwhere id = 1";

string indented  = block.Indent(4);               // four spaces before every line
string quoted    = block.PrefixLines("> ");       // "> select *\n> from users\n> where id = 1"
string back      = quoted.UnprefixLines("> ");    // == block
string outdented = indented.Outdent(2);           // strips up to two leading spaces per line

int a = "a\n".EnsureTrailingNewLine().Length;     // 2 — already terminated, unchanged
int b = "a".EnsureTrailingNewLine().Length;       // 2 — "\n" appended
int c = "a\r\n".RemoveTrailingNewLine().Length;   // 1 — one terminator removed
string joined = "a\r\nb\nc".RemoveLineEndings();  // "abc"
```

| Method | Behaviour |
|---|---|
| `CollapseWhitespace()` | Each run of one or more white-space characters (of any kind) becomes a single U+0020 space. Leading and trailing runs are collapsed, not trimmed. |
| `RemoveWhitespace()` | Removes every white-space character. |
| `NormalizeLineEndings(newline = "\n")` | Rewrites every CRLF, CR, and LF as `newline`. |
| `RemoveLineEndings()` | Removes every CR and LF character (lines run together). |
| `SplitLines(removeEmptyLines = false)` | Lazily yields each line without its terminator; an empty input yields nothing. |
| `Indent(count, indentChar = ' ')` / `Outdent(count, indentChar = ' ')` | Adds `count` copies of `indentChar` to every line, or strips *up to* `count` leading occurrences from each line. |
| `PrefixLines(prefix)` / `UnprefixLines(prefix)` | Adds `prefix` to every line, or removes one occurrence from each line that starts with it. |
| `EnsureTrailingNewLine(newline = "\n")` | Appends `newline` only when the string does not already end with it. |
| `RemoveTrailingNewLine()` | Removes a single trailing CRLF, CR, or LF. |

## Pattern 4 — ordinal predicates and replacement

These are the `StringComparison.Ordinal` / `OrdinalIgnoreCase` forms of the BCL predicates, spelled out so a code review can see the comparison rule at the call site.

```csharp
using Bodu.Extensions;

string header = "Content-Type: application/json";

bool starts = header.StartsWithOrdinalIgnoreCase("content-type");   // true
bool ends   = header.EndsWithOrdinal("json");                      // true
bool has    = header.ContainsOrdinalIgnoreCase("APPLICATION");     // true
bool same   = "JSON".EqualsOrdinalIgnoreCase("json");              // true
bool oneOf  = "GET".IsOneOf("GET", "HEAD", "OPTIONS");             // true — ordinal
bool oneOfI = "get".IsOneOf(StringComparer.OrdinalIgnoreCase, "GET", "HEAD");   // true

string swapped = header.ReplaceOrdinalIgnoreCase("APPLICATION", "text");   // "Content-Type: text/json"
```

| Method | Comparison | Null receiver |
|---|---|---|
| `StartsWithOrdinal(s)` / `EndsWithOrdinal(s)` | Ordinal, case-sensitive | throws |
| `StartsWithOrdinalIgnoreCase(s)` / `EndsWithOrdinalIgnoreCase(s)` / `ContainsOrdinalIgnoreCase(s)` | Ordinal, case-insensitive | throws |
| `EqualsOrdinalIgnoreCase(other)` | Ordinal, case-insensitive; `null == null` is `true` | accepted |
| `IsOneOf(params values)` / `IsOneOf(comparer, params values)` | Ordinal, or the supplied comparer | throws |
| `ReplaceOrdinalIgnoreCase(oldValue, newValue)` | Replaces every case-insensitive ordinal match; `newValue` may be `null` (removes). | throws |

## Pattern 5 — null, empty, and whitespace coalescing

These eight are the only members declared on `string?`. They turn the `string.IsNullOrWhiteSpace` dance into one call.

```csharp
using Bodu.Extensions;

string? missing = null;
string  blank   = "   ";
string  padded  = "  value  ";

string a = missing.DefaultIfNullOrEmpty("(none)");        // "(none)"
string b = blank.DefaultIfNullOrWhiteSpace("(blank)");    // "(blank)"
bool   c = blank.NullIfWhiteSpace() is null;              // true
bool   d = "".NullIfEmpty() is null;                      // true
string? e = padded.TrimToNull();                          // "value"
bool   f = blank.TrimToNull() is null;                    // true
int    g = missing.TrimOrEmpty().Length;                  // 0
bool   h = padded.HasText();                              // true
bool   i = blank.HasText();                               // false
```

| Method | `null` | `""` | `"   "` | `"  x  "` |
|---|---|---|---|---|
| `DefaultIfNullOrEmpty(d)` | `d` | `d` | `"   "` | unchanged |
| `DefaultIfNullOrWhiteSpace(d)` | `d` | `d` | `d` | unchanged |
| `NullIfEmpty()` | `null` | `null` | `"   "` | unchanged |
| `NullIfWhiteSpace()` | `null` | `null` | `null` | unchanged |
| `TrimToNull()` | `null` | `null` | `null` | `"x"` |
| `TrimOrEmpty()` | `""` | `""` | `""` | `"x"` |
| `HasText()` | `false` | `false` | `false` | `true` |
| `EqualsOrdinalIgnoreCase(other)` | compares (`null` equals only `null`) | | | |

## Pattern 6 — affixes and removal

```csharp
using Bodu.Extensions;

string withSlash = "logs".EnsureEndsWith("/");             // "logs/"
string unchanged = "logs/".EnsureEndsWith("/");            // "logs/" — not doubled
string rooted    = "logs".EnsureStartsWith("/");           // "/logs"
string bare      = "v1.2.3".RemovePrefix("v");             // "1.2.3"
string noExt     = "report.PDF".RemoveSuffix(".pdf", StringComparison.OrdinalIgnoreCase);   // "report"
string stripped  = "a-b-a".Remove("a");                    // "-b-" — every occurrence
string many      = "x=1; y=2".RemoveMany("=", ";");        // "x1 y2"
string replaced  = "Hello {name}, {greeting}".ReplaceMany(new Dictionary<string, string>
{
    ["{name}"] = "Ada",
    ["{greeting}"] = "welcome back",
});                                                        // "Hello Ada, welcome back"
```

| Method | Behaviour |
|---|---|
| `EnsureStartsWith(prefix, comparison)` / `EnsureEndsWith(suffix, comparison)` | Adds the affix only when it is not already present under `comparison`. |
| `RemovePrefix(prefix, comparison)` / `RemoveSuffix(suffix, comparison)` | Removes a **single** leading or trailing occurrence when present. |
| `Remove(valueToRemove, comparison)` | Removes **every** occurrence. |
| `RemoveMany(params valuesToRemove)` | Removes every occurrence of each entry, applied sequentially in array order (ordinal). |
| `ReplaceMany(replacements)` | Applies each key → value replacement sequentially in the dictionary's enumeration order (ordinal). Because the passes are sequential, a later key can match text produced by an earlier replacement — order the dictionary deliberately. |

## Pattern 7 — character filtering

The `Keep*` family retains only the named Unicode category; the `Remove*` family drops it. `KeepWhere` / `RemoveWhere` take an arbitrary `Func<char, bool>`.

```csharp
using Bodu.Extensions;

string input = "Café Ünïcode #42 (draft)!";

string digits   = input.KeepDigits();               // "42"
string letters  = input.KeepLetters();              // "CaféÜnïcodedraft"
string alnum    = input.KeepLettersAndDigits();     // "CaféÜnïcode42draft"
string ascii    = input.RemoveDiacritics();         // "Cafe Unicode #42 (draft)!"
string noPunct  = input.RemovePunctuation();        // "Café Ünïcode 42 draft"
string noDigits = input.RemoveDigits();             // "Café Ünïcode # (draft)!"
string noCtrl   = "tab\there".RemoveControlCharacters();   // "tabhere"
string upper    = input.KeepWhere(char.IsUpper);    // "CÜ"
string noNums   = "a1b2".RemoveWhere(char.IsDigit); // "ab"
```

| Method | Keeps / removes |
|---|---|
| `KeepDigits()` / `RemoveDigits()` | Unicode decimal digits (`char.IsDigit`). |
| `KeepLetters()` | Unicode letters (`char.IsLetter`). |
| `KeepLettersAndDigits()` | Letters and digits. |
| `RemovePunctuation()` | Unicode punctuation (`char.IsPunctuation`) — note that `#` and `!` are punctuation, so they go. |
| `RemoveControlCharacters()` | Unicode control characters (tab, CR, LF, …). |
| `RemoveDiacritics()` | Decomposes to form D and drops combining marks, so `é` → `e`; base letters and everything else are preserved. |
| `KeepWhere(predicate)` / `RemoveWhere(predicate)` | Your predicate, per `char`. |

## Pattern 8 — identifier casing

Nine casing conversions share one acronym-aware tokenizer: the input is split at separators (spaces, punctuation, underscores, hyphens), at lower-to-upper boundaries, at digit-to-letter boundaries, and at acronym-to-word boundaries (`XMLHttpRequest` → `XML`, `Http`, `Request`), then each word is re-cased and re-joined.

```csharp
using Bodu.Extensions;

string title = "parse HTTP request from XMLHttpRequest v2";

string camel    = title.ToCamelCase();      // "parseHttpRequestFromXmlHttpRequestV2"
string pascal   = title.ToPascalCase();     // "ParseHttpRequestFromXmlHttpRequestV2"
string snake    = title.ToSnakeCase();      // "parse_http_request_from_xml_http_request_v2"
string kebab    = title.ToKebabCase();      // "parse-http-request-from-xml-http-request-v2"
string constant = title.ToConstantCase();   // "PARSE_HTTP_REQUEST_FROM_XML_HTTP_REQUEST_V2"
string dot      = title.ToDotCase();        // "parse.http.request.from.xml.http.request.v2"
string train    = title.ToTrainCase();      // "Parse-Http-Request-From-Xml-Http-Request-V2"
string titled   = title.ToTitleCase();      // "Parse Http Request From Xml Http Request V2"
string sentence = "the API of the iPhone. a second sentence".ToSentenceCase();
                                            // "The api of the iPhone. A second sentence"
```

The parameterless overloads use <xref:Bodu.Extensions.WordCasingOptions.Default> — invariant culture, the built-in acronym list recognised for *splitting*, mixed-case brand words (`iPhone`, `eBay`) preserved, and every word re-cased (which is why `HTTP` becomes `Http` above). Pass a <xref:Bodu.Extensions.WordCasingOptions> to change that:

```csharp
using System.Globalization;
using Bodu.Extensions;

var options = new WordCasingOptions
{
    PreserveAcronyms = true,             // keep known acronyms upper-case in title / sentence case
    LowerCaseMinorWords = true,          // "of", "the", "for", … stay lower-case inside a title
    Acronyms = new[] { "HTTP", "XML", "API", "SQL" },
    Culture = CultureInfo.InvariantCulture,
};

string a = "the state of the API".ToTitleCase(options);   // "The State of the API"
string b = "the state of the API".ToTitleCase();          // "The State Of The Api"
string c = "SQL server. XML files.".ToSentenceCase(SentenceCaseOptions.PreserveAcronyms);   // "SQL server. XML files."
string d = "SQL server. XML files.".ToSentenceCase();     // "Sql server. Xml files."
string e = "XMLHttpRequest".ToSnakeCase();                // "xml_http_request" — splitting is acronym-aware regardless
```

| Option | Default | Effect |
|---|---|---|
| `Acronyms` | 38 common technology acronyms | Tokens recognised as acronyms when splitting run-together words and (with `PreserveAcronyms`) kept upper-case. |
| `MinorWords` | 21 English connective words | The words `LowerCaseMinorWords` may down-case inside a title. |
| `Culture` | `CultureInfo.InvariantCulture` | Culture for `ToUpper` / `ToLower` (matters for `tr-TR` dotted I, for example). |
| `PreserveAcronyms` | `true` | Only `ToTitleCase(options)` and `ToSentenceCase(options)` honour it; the identifier casings always normalize to one shape. |
| `PreserveMixedCaseWords` | `true` | `iPhone`, `eBay`, and similar one-lower-then-upper words survive re-casing. |
| `LowerCaseMinorWords` | `false` | Title case only. |

`ToTitleCase` and `ToSentenceCase` also accept the lighter flag enums <xref:Bodu.Extensions.TitleCaseOptions> (`None`, `LowerCaseSmallWords`, `PreserveAcronyms`) and <xref:Bodu.Extensions.SentenceCaseOptions> (`None`, `PreserveAcronyms`) when you do not need a custom word list or culture.

## Pattern 9 — slugs, identifiers, and safe names

```csharp
using Bodu.Extensions;

string slug = "Crème Brûlée & Co. — 2024 Edition!".ToSlug();   // "creme-brulee-co-2024-edition"

string shortSlug = "Crème Brûlée & Co. — 2024 Edition!".ToSlug(new SlugOptions
{
    Separator = '_',
    Lowercase = false,
    MaxLength = 16,          // truncated at a separator boundary, never ends with '_'
});                                                              // "Creme_Brulee_Co"

string id       = "first name (legacy)".ToIdentifier();                        // "firstnamelegacy" — casing preserved
string pascalId = "first name (legacy)".ToIdentifier(IdentifierCase.Pascal);   // "FirstNameLegacy"
string camelId  = "first name (legacy)".ToIdentifier(IdentifierCase.Camel);    // "firstNameLegacy"
string snakeId  = "first name (legacy)".ToIdentifier(IdentifierCase.Snake);    // "first_name_legacy"
string leading  = "2fast".ToIdentifier();                                      // "_2fast" — cannot start with a digit

bool ok  = "_ok1".IsValidIdentifier();   // true
bool bad = "1abc".IsValidIdentifier();   // false

string file = "report: q1/q2 <final>.pdf".ToSafeFileName();   // on Linux: "report: q1_q2 <final>.pdf"
```

| Member | Behaviour |
|---|---|
| `ToSlug()` / `ToSlug(SlugOptions)` | Diacritics normalized, punctuation dropped, words joined by <xref:Bodu.Extensions.SlugOptions.Separator> (`-`), lower-cased by default. `MaxLength` (0 = unlimited) truncates at a separator boundary so the slug never ends with a dangling separator. |
| `ToIdentifier()` | Drops characters not permitted in a C# identifier and prefixes `_` when the result would start with a digit; original casing is kept. |
| `ToIdentifier(IdentifierCase)` | As above, then reshapes the detected words to <xref:Bodu.Extensions.IdentifierCase> `Camel`, `Pascal`, or `Snake` (`Preserve` is the default). |
| `IsValidIdentifier()` | A letter or `_` followed by letters, digits, `_`, or connector punctuation. It is a syntactic check — it does not reject C# keywords. |
| `ToSafeFileName()` | Replaces every character in <xref:System.IO.Path.GetInvalidFileNameChars> with `_`. The invalid set is **platform-dependent**: on Windows `:`, `<`, `>`, `"`, `|`, `?`, `*` and the control characters are replaced too; on Linux only `/` and NUL are. |
| `ToSafePathSegment()` | The same set plus both directory separators, so the result can never escape into a parent or child segment. |

## Pattern 10 — truncation, slicing, Base64, and parsing

```csharp
using Bodu.Extensions;

string text = "The quick brown fox jumps over the lazy dog";

string t1 = text.Truncate(9);                // "The quick"
string t2 = text.Truncate(12, "...");        // "The quick..." — the ellipsis counts toward maxLength
string t3 = text.TruncateMiddle(15);         // "The qui…azy dog" — keeps both ends, 15 chars total
string s1 = text.SliceSafe(40);              // "dog"
string s2 = text.SliceSafe(40, 100);         // "dog" — length clamped to what remains
string s3 = text.SliceSafe(99);              // "" — start past the end clamps to empty

string b64  = "The quick".ToBase64();                 // "VGhlIHF1aWNr" (UTF-8 by default)
string back = "VGhlIHF1aWNr".FromBase64ToString();    // "The quick"

int    n  = "42".Parse<int>() + 1;                    // 43 — any IParsable<T>, invariant culture
bool   ok = "nope".TryParse<int>(out int value);      // false
```

| Method | Behaviour |
|---|---|
| `Truncate(maxLength)` | Returns the first `maxLength` characters; shorter input is returned unchanged. |
| `Truncate(maxLength, ellipsis)` | As above, but the result *including* `ellipsis` fits in `maxLength`; `maxLength` shorter than the ellipsis throws `ArgumentOutOfRangeException`. |
| `TruncateMiddle(maxLength, separator = "…")` | Keeps the head and tail and inserts `separator` between them. |
| `SliceSafe(startIndex)` / `SliceSafe(startIndex, length)` | `Substring` that clamps out-of-range arguments instead of throwing. |
| `ToBase64(encoding = null)` / `FromBase64ToString(encoding = null)` | Encodes with `encoding` (UTF-8 when `null`) and Base64; the reverse decodes and re-interprets. |
| `Parse<T>()` / `TryParse<T>(out T)` | <xref:System.IParsable`1> under <xref:System.Globalization.CultureInfo.InvariantCulture>; `ParseSpan<T>` / `TryParseSpan<T>` route through <xref:System.ISpanParsable`1> to avoid an intermediate string. |

## Choosing between similar members

- **`After` vs `Substring`** — `After` returns `null` when the marker is absent instead of throwing on a computed index; prefer it whenever the marker is data-dependent.
- **`CollapseWhitespace` vs `Trim`** — collapsing keeps one space at each end when the input had leading or trailing whitespace; call `Trim()` afterwards if you want none.
- **`ToTitleCase(options)` vs `TextInfo.ToTitleCase`** — the BCL method only upper-cases each first letter and knows nothing about acronyms or minor words.
- **`ToSlug` vs `ToKebabCase`** — both hyphenate, but only `ToSlug` strips diacritics and punctuation and enforces `MaxLength`; use kebab case for identifiers, slug for URLs.
- **`ToSafeFileName` vs `ToSafePathSegment`** — the second additionally neutralizes `/` and `\` on every platform.
- **Sorting and comparing** — none of these methods compare strings *for ordering*. For human-friendly ordering of `file2` before `file10`, use <xref:Bodu.Extensions.NaturalStringComparer> — see [Natural string comparer](natural-string-comparer.md).

## API summary

| Group | Members |
|---|---|
| Substring | `After`, `AfterLast`, `Before`, `BeforeLast`, `Between` |
| Wrapping | `Brace`, `Bracket`, `Parenthesize`, `Quote`, `SingleQuote`, `Wrap`, `Unwrap` |
| Whitespace and lines | `CollapseWhitespace`, `RemoveWhitespace`, `NormalizeLineEndings`, `RemoveLineEndings`, `SplitLines`, `Indent`, `Outdent`, `PrefixLines`, `UnprefixLines`, `EnsureTrailingNewLine`, `RemoveTrailingNewLine` |
| Ordinal predicates | `StartsWithOrdinal`, `StartsWithOrdinalIgnoreCase`, `EndsWithOrdinal`, `EndsWithOrdinalIgnoreCase`, `ContainsOrdinalIgnoreCase`, `EqualsOrdinalIgnoreCase`, `IsOneOf`, `ReplaceOrdinalIgnoreCase` |
| Null / empty | `DefaultIfNullOrEmpty`, `DefaultIfNullOrWhiteSpace`, `NullIfEmpty`, `NullIfWhiteSpace`, `TrimToNull`, `TrimOrEmpty`, `HasText` |
| Affixes and removal | `EnsureStartsWith`, `EnsureEndsWith`, `RemovePrefix`, `RemoveSuffix`, `Remove`, `RemoveMany`, `ReplaceMany` |
| Filtering | `KeepDigits`, `KeepLetters`, `KeepLettersAndDigits`, `KeepWhere`, `RemoveDigits`, `RemovePunctuation`, `RemoveControlCharacters`, `RemoveDiacritics`, `RemoveWhere` |
| Casing | `ToCamelCase`, `ToPascalCase`, `ToSnakeCase`, `ToKebabCase`, `ToConstantCase`, `ToDotCase`, `ToTrainCase`, `ToTitleCase`, `ToSentenceCase` (each with a `WordCasingOptions` overload; title and sentence case also take `TitleCaseOptions` / `SentenceCaseOptions`) |
| Identifiers and paths | `ToSlug` (+ `SlugOptions`), `ToIdentifier` (+ `IdentifierCase`), `IsValidIdentifier`, `ToSafeFileName`, `ToSafePathSegment` |
| Miscellaneous | `Truncate`, `TruncateMiddle`, `SliceSafe`, `ToBase64`, `FromBase64ToString`, `Parse<T>`, `TryParse<T>`, `ParseSpan<T>`, `TryParseSpan<T>` |

## Where to go next

- [Natural string comparer](natural-string-comparer.md) — ordering strings with embedded numbers the way a person would.
- [Date and time extensions](date-extensions.md) — the sibling `DateTime` / `DateOnly` surface in the same namespace.
- [Numeric, enum, array, span, and stream extensions](numeric-enum-stream-extensions.md) — the rest of `Bodu.Extensions`.
- [Bodu.Core introduction](../../docs/core/index.md) — the package's namespaces and headline types.
- [`Bodu.Extensions` API reference](xref:Bodu.Extensions) — every member with full signatures.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
