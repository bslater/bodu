---
uid: Bodu.Extensions
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

**Bodu.Extensions** is the extension-method surface of `Bodu.Core`. It carries 15+ static classes covering date / time, numeric, span, array, string, enum, comparable, sequence, and stream operations — the framework-style helpers that keep ceremony out of hot paths in the rest of the Bodu solution and in consumer code.

This is the single highest-leverage namespace in `Bodu.Core` by surface area. Reach for it when you need a `DateTime` calendar / week operation that the BCL doesn't ship, when you need a `decimal`-aware significant-digit rounding helper, when you need string `Slice` / `Wrap` / `Quote` / `Slug` / case-conversion utilities, or when you need bit / byte rotation and reversal across the unsigned integer types.

## Static documentation

- **[Bodu.Core introduction](~/docs/core/index.md)** — `Bodu.Extensions` summary table.
- **[Bodu.Core getting started](~/docs/core/getting-started.md)** — date-arithmetic minimal samples.

## Key types grouped by concern

**Date and time**

- <xref:Bodu.Extensions.DateTimeExtensions> — first / last / next / previous day-of-week within month / quarter / year, ISO week-of-year, day name, weekday tests, midday, end-of-day, truncation. 50+ methods.
- <xref:Bodu.Extensions.DateOnlyExtensions> — `DateOnly`-specific equivalents plus `Age` calculation.
- <xref:Bodu.Extensions.DateTimeFormatInfoExtensions> — culture-aware day-of-week and month-name helpers.
- <xref:Bodu.Extensions.IQuarterDefinitionProvider>, <xref:Bodu.Extensions.IWeekendDefinitionProvider>, <xref:Bodu.Extensions.IWeekendDefinitionProviderExtensions> — pluggable calendar-shape providers for non-Gregorian or fiscal quarters and non-Saturday/Sunday weekend conventions.
- <xref:Bodu.Extensions.WorkingDaysOfWeekExtensions>, <xref:Bodu.WorkingDaysOfWeek> — working-day bitmask helpers.

**Calendar-shape enums**

- <xref:Bodu.CalendarQuarterDefinition> — `Fiscal`, `Calendar`.
- <xref:Bodu.DateTimeResolution> — truncation resolution.
- <xref:Bodu.FiscalWeekPattern> — fiscal-week enumeration.
- <xref:Bodu.Extensions.WeekOfMonthOrdinal> — `First`, `Second`, `Third`, `Fourth`, `Fifth`, `Last`.

**Numeric**

- <xref:Bodu.Extensions.NumericExtensions> — `Digits`, `GetBytes`, `GreatestCommonDivisor`, `IsPrime`, `LeastCommonMultiple`, `ReverseBits`, `ReverseBytes`, `ReverseWords`, `RotateBitsLeft` / `Right`, `RoundToSignificantDigits`, unchecked arithmetic helpers.

**Span, array, stream, buffer**

- <xref:Bodu.Extensions.SpanExtensions> — `AsReadOnly`, `Reverse`.
- <xref:Bodu.Extensions.ArrayExtensions> — `Clear`, `Copy`, `Pad`, `Reverse`, `Slice`, `ToMatrix`.
- <xref:Bodu.Extensions.StreamExtensions> — `ReadAllBytes` / `ReadAllBytesAsync` / `WriteAllBytes` / `WriteAllBytesAsync`.
- <xref:Bodu.Extensions.BufferConverter> — `CopyTo`, `Read`, `SwapEndian`, `ToArray` over byte buffers and primitive types.

**Strings**

- <xref:Bodu.Extensions.StringExtensions> — `After`, `Before`, `Between`, `Brace`, `Bracket`, `CollapseWhitespace`, `Contains` / `EndsWith` / `StartsWith` variants, `EnsureEndsWith` / `EnsureStartsWith`, `Indent`, `IsValidIdentifier`, `Keep` / `Remove` variants, `Normalize`, `Outdent`, `Parenthesize`, `Parse`, `PrefixLines`, `Quote`, `RemoveControlCharacters`, `RemoveDiacritics`, `Slice`, case conversions (`ToCamelCase`, `ToPascalCase`, `ToSnakeCase`, `ToKebabCase`, …), `Truncate`, `Unwrap`, `Wrap`, slug generation.
- <xref:Bodu.IdentifierCase> — case convention enum used by `IsValidIdentifier` / case conversions.
- <xref:Bodu.Extensions.SentenceCaseOptions>, <xref:Bodu.Extensions.TitleCaseOptions>, <xref:Bodu.Extensions.WordCasingOptions>, <xref:Bodu.Extensions.SlugOptions> — option flags consumed by the casing helpers.

**Comparable**

- <xref:Bodu.Extensions.IComparableExtensions> — `AtLeast`, `AtMost`, `Clamp`, `IsBetween`, `IsGreaterThan`, `Max`, `Min`, …
- <xref:Bodu.Extensions.ComparableHelper> — `Coalesce`, `Max`, `Min` over nullable comparables.

**Enums**

- <xref:Bodu.Extensions.EnumExtensions> — flag operations: `SetFlag`, `ClearFlag`, `HasAllFlags`, `HasAnyFlag`, `ToggleFlag`, plus bit-conversion helpers between flag enums and their underlying integer representations.

## Example

```csharp
using Bodu.Extensions;

// Calendar arithmetic — first Monday of Q3, ISO week, age.
DateTime monday = new DateTime(2026, 7, 1).GetFirstDateOfWeek(DayOfWeek.Monday);
DateTime endQ   = DateTime.Today.LastDateOfQuarter();
int isoWeek    = DateTime.Today.IsoWeekOfYear();
int age        = new DateOnly(1990, 5, 4).Age();

// Numeric — bit operations and rounding.
uint rotated   = 0xDEADBEEFu.RotateBitsLeft(8);
uint reversed  = 0x12345678u.ReverseBytes();         // 0x78563412
decimal scaled = 1234.5678m.RoundToSignificantDigits(3);  // 1230

// Strings — slug, case conversion, between.
string slug = "Hello, World!".ToSlug();              // "hello-world"
string snake = "FirstName".ToSnakeCase();            // "first_name"
string url = "https://example.com/a/b?x=1".Between("//", "/");  // "example.com"
```

## Notes

- **Pure extensions.** Every type here is a static class. There are no instance types and no allocations on the helpers themselves; cost is the work they perform on their arguments.
- **Culture-aware where it matters.** Date / time / culture-sensitive string helpers accept an `IFormatProvider` or `CultureInfo`. The defaults follow the BCL convention (current culture for parsing, invariant culture for format).
- **Argument validation.** All public extension methods go through the centralised <xref:Bodu.ThrowHelper> validation surface for null / range / array-length checks.
- **Companion namespaces.** Sequence-shaping helpers for `IEnumerable<T>` / `IList<T>` live in <xref:Bodu.Collections.Extensions> and <xref:Bodu.Collections.Generic.Extensions>. Globalisation-specific date helpers used by the calendar package live in <xref:Bodu.Globalization.Extensions>.
- **See also:** the [Bodu.Core introduction](~/docs/core/index.md), the [`DateTimeExtensions`](xref:Bodu.Extensions.DateTimeExtensions) / [`StringExtensions`](xref:Bodu.Extensions.StringExtensions) / [`NumericExtensions`](xref:Bodu.Extensions.NumericExtensions) full references.
