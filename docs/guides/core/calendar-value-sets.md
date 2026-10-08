---
title: Calendar value sets
---

# Calendar value sets

Bodu.Core has an immutable set type for each calendar value a schedule selects from:

| Type | Values | Default text form |
|---|---|---|
| [`DayOfWeekSet`](day-of-week-set.md) | the seven `DayOfWeek` values | a seven-character mask, `"_MTWTF_"` |
| `MonthSet` | months 1 to 12 | a list of values and ranges, `"1-3,12"` |
| `DayOfMonthSet` | days of the month 1 to 31 | the same list form |
| `HourSet` | hours 0 to 23 | the same list form |
| `MinuteSet` | minutes 0 to 59 | the same list form |
| `SecondSet` | seconds 0 to 59 | the same list form |

A set never allocates, and testing a value, adding or removing one, or combining two sets is a single bitwise
operation. All six implement [`ICalendarValueSet<TSelf, TValue>`](#pattern-6---work-with-any-set), so code written
once works with any of them. The five numeric sets share one shape, which this page describes; `DayOfWeekSet` has the
same members over `DayOfWeek`, plus its working-week presets and mask formats.

## Pattern 1 - build and test a set

<!-- run -->
```csharp
using Bodu;

var quarterStarts = new MonthSet(1, 4, 7, 10);
var businessHours = new HourSet(9, 10, 11, 12, 13, 14, 15, 16, 17);

DateTime now = DateTime.Now;
bool due = quarterStarts.Contains(now.Month) && businessHours.Contains(now.Hour);

HourSet extended = businessHours.With(18).Without(12);
Console.WriteLine(extended.Count);   // 9
```

`Contains` answers `false` for a value outside the type's range, such as month 13 or hour -1, rather than throwing,
which keeps a test against a `DateTime` component to a comparison and a bit test. The constructor, `With`, `Without`
and `FromUInt64` throw `ArgumentOutOfRangeException` for such a value.

`DayOfMonthSet` holds day numbers and does not know the month: a set that contains 31 never matches a date in a 30-day
month. `SecondSet` stops at 59, since `DateTime` has no leap second.

## Pattern 2 - parse and format the list

The text form lists the values in ascending order, separated by commas, with each run of two or more consecutive values
written as an inclusive range. The empty set is the empty string. `ToString` writes that canonical form, and `Parse`
reads any list of values and ranges: in any order, with whitespace around the items, and with repeats and overlaps.

<!-- run -->
```csharp
using Bodu;

MonthSet q1AndDecember = MonthSet.Parse("1-3,12");
MonthSet sameMonths    = MonthSet.Parse("12, 3, 1-2, 2");

Console.WriteLine(q1AndDecember == sameMonths);           // True
Console.WriteLine(sameMonths.ToString());                 // 1-3,12
Console.WriteLine(new MinuteSet(0, 15, 30, 45));          // 0,15,30,45

bool ok = SecondSet.TryParse("*/10", out SecondSet result); // false: steps are not part of the form
```

A value outside the range, a descending range such as `"3-1"`, a sign, a step, or an empty item fails: `Parse` throws
`FormatException` naming the text and the range, and `TryParse` returns `false` with the empty set. Each type
implements `IParsable<TSelf>`; its members ignore the format provider, because the text does not depend on culture.

## Pattern 3 - write and read the other formats

`ToString(format)` writes a set in one of three formats, and `ParseExact` and `TryParseExact` read text in the format
you name:

| Format | Text | January to March and December |
|---|---|---|
| `G`, the default | the list, each run of two or more consecutive values as a range | `1-3,12` |
| `L` | every selected value, without ranges | `1,2,3,12` |
| `B`, `0`, `1`, `01` | binary, one digit per value of the range, lowest first, `1` for a selected value | `111000000001` |

`MonthSet` also writes a mask of the months' initials, January first, as `DayOfWeekSet` does for days:

| Format | Mask | January to March and December |
|---|---|---|
| `J` | each selected month's initial in its place, `_` for a month not selected | `JFM________D` |
| `E`, `U`, `D`, `A` | the same, with a space, `_`, `-` or `*` for a month not selected | `JFM--------D` for `D` |
| `JE`, `JU`, `JD`, `JA` | the same as `E`, `U`, `D` and `A` | `JFM--------D` for `JD` |

The format letters, and a mask's initials, are read in either case. `ParseExact` with `J` reads a mask with any one
placeholder throughout, and with a format that names a placeholder, that placeholder alone. An unsupported format
throws `FormatException` from `ToString` and `ParseExact`, and makes `TryParseExact` return `false`. Each type also
implements `IFormattable`, so `$"{set:L}"` and `string.Format("{0:B}", set)` apply the format.

<!-- run -->
```csharp
using Bodu;

var months = new MonthSet(1, 2, 3, 12);

Console.WriteLine(months.ToString("L"));        // 1,2,3,12
Console.WriteLine(months.ToString("B"));        // 111000000001
Console.WriteLine(months.ToString("J"));        // JFM________D
Console.WriteLine($"{new HourSet(9, 17):B}");   // 000000000100000001000000

MonthSet fromBinary  = MonthSet.ParseExact("111000000001", "B");
MonthSet fromLetters = MonthSet.ParseExact("jfm--------d", "J");
bool rangeUnderL     = MonthSet.TryParseExact("1-3,12", "L", out _);   // false: L has no ranges
```

`Parse` and `TryParse` read every format without being told which, testing the forms in a fixed order:

1. Text that is exactly one `0` or `1` per value of the range, as given, is binary: twelve digits for a `MonthSet`,
   sixty for a `MinuteSet`.
2. For a `MonthSet`, twelve characters, each a month's initial in its place or a placeholder, the same one throughout,
   are the letter mask.
3. Anything else is a list.

So binary text is binary even where it would also read as a list: to a `MonthSet`, `"000000000001"` is December,
though as a list it would be January. Zeros and ones of any other length are a list, so `"10"` is October, and binary
text with whitespace around it is read as a list too. Text that fits none of the forms fails with the list's message.
To read one format only, call `ParseExact`.

### Spans and UTF-8

Every text form is ASCII, so the sets read and write it as characters or as UTF-8 bytes alike:

- `TryFormat` writes the text `ToString(format)` returns into a span of characters or of UTF-8 bytes, without
  allocating. When the span is too short it returns `false` and writes nothing; an unsupported format throws
  `FormatException`, as it does from `ToString`.
- `Parse` and `TryParse` also read a `ReadOnlySpan<char>`, or a `ReadOnlySpan<byte>` of UTF-8, and `ParseExact` and
  `TryParseExact` a `ReadOnlySpan<char>`. They accept what the `string` overloads accept and throw the same messages;
  bytes that are not valid UTF-8 are never a set.
- The sets implement `ISpanFormattable` and `IUtf8SpanFormattable`, so string interpolation, `StringBuilder` and
  `Utf8.TryWrite` format them through `TryFormat`, without an intermediate string.

<!-- run -->
```csharp
using Bodu;

var months = new MonthSet(1, 2, 3, 12);

Span<char> chars = stackalloc char[32];
bool written = months.TryFormat(chars, out int charsWritten, "L");   // true: chars[..charsWritten] is "1,2,3,12"

Span<byte> utf8 = stackalloc byte[32];
written = months.TryFormat(utf8, out int bytesWritten, "J");         // true: the bytes of "JFM________D"

Span<char> small = stackalloc char[4];
bool fits = months.TryFormat(small, out _);                          // false: "1-3,12" needs six characters

MonthSet fromUtf8  = MonthSet.Parse("1-3,12"u8);
MonthSet fromChars = MonthSet.ParseExact("111000000001".AsSpan(), "B");
```

## Pattern 4 - combine sets

`|` is union, `&` intersection, `^` symmetric difference, and `~` the complement within the type's range:

<!-- run -->
```csharp
using Bodu;

HourSet mornings  = HourSet.Parse("6-11");
HourSet afternoon = HourSet.Parse("12-17");

HourSet daytime   = mornings | afternoon;      // 6-17
HourSet overnight = ~daytime;                  // 0-5,18-23
HourSet overlap   = mornings & afternoon;      // empty

Console.WriteLine(overnight);                  // 0-5,18-23
Console.WriteLine(overlap == HourSet.Empty);   // True
```

Two sets are equal when they select the same values. The sets are not ordered; to test whether one contains another,
use `(a & b) == b`.

## Pattern 5 - enumerate and read the bits

`foreach` yields the selected values in ascending order without allocating. `ToUInt64` returns the bits and
`FromUInt64` builds a set from them: bit `n` selects the type's smallest value plus `n`, so bit 0 is month 1 in a
`MonthSet` and hour 0 in an `HourSet`.

<!-- run -->
```csharp
using Bodu;

foreach (int day in DayOfMonthSet.Parse("1,15,28-31"))
    Console.WriteLine(day);                    // 1, 15, 28, 29, 30, 31

ulong bits = new MonthSet(1, 12).ToUInt64();   // 0b1000_0000_0001: bits 0 and 11
MonthSet again = MonthSet.FromUInt64(bits);    // January and December
```

## Pattern 6 - work with any set

`ICalendarValueSet<TSelf, TValue>` declares what the six sets share: `Empty` and `All`, `Count`, `Contains`, `With`
and `Without`, `FromUInt64` and `ToUInt64`, `ParseExact` and `TryParseExact` over strings and spans of characters, and
`ToString(string)`, together with the operators, equality, enumeration, parsing from strings, spans and UTF-8
(`IParsable<TSelf>`, `ISpanParsable<TSelf>`, `IUtf8SpanParsable<TSelf>`) and formatting to them (`IFormattable`,
`ISpanFormattable`, `IUtf8SpanFormattable`). `TValue` is `DayOfWeek` for `DayOfWeekSet` and `int` for the others. A
method generic over the interface takes any of them:

<!-- run -->
```csharp
using Bodu;

static string Describe<TSet, TValue>(TSet set)
    where TSet : ICalendarValueSet<TSet, TValue> =>
    set == TSet.All ? "every value" : $"{set.Count} selected: {set}";

static TSet ComplementOf<TSet, TValue>(string text)
    where TSet : ICalendarValueSet<TSet, TValue> =>
    ~TSet.Parse(text, null);

Console.WriteLine(Describe<MonthSet, int>(new MonthSet(1, 4, 7, 10)));      // 4 selected: 1,4,7,10
Console.WriteLine(Describe<DayOfWeekSet, DayOfWeek>(DayOfWeekSet.Weekend)); // 2 selected: S_____S
Console.WriteLine(ComplementOf<HourSet, int>("0-5,18-23"));                 // 6-17
```

## How the sets are stored

Each set is a `readonly struct` holding one `ulong`. Bit `n` selects the value at offset `n` from the smallest value of
the type's range, and every bit past the range is clear, so a range has at most 64 values and two sets are equal
exactly when their bits are. These are the bits `ToUInt64` returns and `FromUInt64` takes; `FromUInt64` rejects a bit
past the range rather than dropping it. No set converts to or from a number implicitly or explicitly: call `ToUInt64`
and `FromUInt64`.

A membership test is therefore a range check and a bit test, `Count` is one population count, `|`, `&` and `^` are one
bitwise instruction each, and `~` is one more to mask the complement to the range.

## API summary

Each of `MonthSet`, `DayOfMonthSet`, `HourSet`, `MinuteSet` and `SecondSet` has these members:

| Member | Description |
|---|---|
| `Empty`, `All` | The set of no value and the set of every value in the range. |
| constructor, `params int[]` | The set of the values given; `null` for none. |
| `Count` | The number of values selected. |
| `Contains(int)` | Whether the value is selected; `false` outside the range. |
| `With(int)`, `Without(int)` | A set with the value added or removed. |
| `FromUInt64(ulong)`, `ToUInt64()` | The set's bits, bit `n` for the smallest value plus `n`. |
| `Parse`, `TryParse` | Read a list of values and ranges, or binary text; `MonthSet` also its letter mask. Each takes a `string`, a `ReadOnlySpan<char>` or a `ReadOnlySpan<byte>` of UTF-8. |
| `ParseExact`, `TryParseExact` | Read text in the format named, from a `string` or a `ReadOnlySpan<char>`. |
| `ToString()` | Write the canonical list. |
| `ToString(string)`, `ToString(string, IFormatProvider)` | Write the list (`G`), every value (`L`) or binary text (`B`); `MonthSet` also its letter mask (`J`). |
| `TryFormat(Span<char>, ...)`, `TryFormat(Span<byte>, ...)` | Write the text `ToString(string)` returns into a span of characters or of UTF-8 bytes, without allocating. |
| `\|`, `&`, `^`, `~`, `==`, `!=` | Union, intersection, symmetric difference, complement and equality. |
| `GetEnumerator()` | The selected values in ascending order, without allocating. |

Each also implements `ICalendarValueSet<TSelf, int>`, as `DayOfWeekSet` implements
`ICalendarValueSet<DayOfWeekSet, DayOfWeek>`.

## Where to go next

- [DayOfWeekSet](day-of-week-set.md) - the set of days of the week, with working-week presets and mask formats.
- [Date and time extensions](date-extensions.md) - the date arithmetic the sets pair with.
- [RRULE recurrence rules](../recurrence/rrule.md#pattern-3---build-a-rule-fluently) - `RecurrenceRuleBuilder` takes each
  set for the rule part it expresses, such as `ByMonth(MonthSet)` and `ByDay(DayOfWeekSet)`.
- **[Core Foundations guides](../topics/core-foundations.md)** - every guide in this topic.
