---
title: Calendar value sets
---

# Calendar value sets

Bodu.Core has an immutable set type for each calendar value a schedule selects from:

| Type | Values | Text form |
|---|---|---|
| [`DayOfWeekSet`](day-of-week-set.md) | the seven `DayOfWeek` values | a seven-character mask, `"_MTWTF_"` |
| `MonthSet` | months 1 to 12 | a list of values and ranges, `"1-3,12"` |
| `DayOfMonthSet` | days of the month 1 to 31 | the same list form |
| `HourSet` | hours 0 to 23 | the same list form |
| `MinuteSet` | minutes 0 to 59 | the same list form |
| `SecondSet` | seconds 0 to 59 | the same list form |

Each set holds one bit per value, so it never allocates, and testing a value, adding or removing one, or combining two
sets is a single bitwise operation. The five numeric sets share one shape, which this page describes; `DayOfWeekSet`
has the same members over `DayOfWeek`, plus its working-week presets and mask formats.

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

## Pattern 3 - combine sets

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

## Pattern 4 - enumerate and read the bits

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
| `Parse(string)`, `TryParse(string, out TSelf)` | Read a list of values and ranges. |
| `ToString()` | Write the canonical list. |
| `\|`, `&`, `^`, `~`, `==`, `!=` | Union, intersection, symmetric difference, complement and equality. |
| `GetEnumerator()` | The selected values in ascending order, without allocating. |

## Where to go next

- [DayOfWeekSet](day-of-week-set.md) - the set of days of the week, with working-week presets and mask formats.
- [Date and time extensions](date-extensions.md) - the date arithmetic the sets pair with.
- **[Core Foundations guides](../topics/core-foundations.md)** - every guide in this topic.
