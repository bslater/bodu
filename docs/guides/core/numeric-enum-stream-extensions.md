---
title: Numeric, enum, array, span, and stream extensions
---

# Numeric, enum, array, span, and stream extensions

Beyond the [string](string-extensions.md) and [date](date-extensions.md) families, `Bodu.Extensions` carries a set of small, focused helper classes for numbers, comparables, enums, arrays, spans, streams, and raw byte buffers. None of them holds state — every member is a pure function of its arguments — so they are safe to call from any thread.

| Class | What it adds |
|---|---|
| <xref:Bodu.Extensions.NumericExtensions> | Digit manipulation, primality, GCD / LCM, bit / byte / word reversal and rotation, significant-digit rounding, byte extraction. |
| <xref:Bodu.Extensions.ComparableExtensions> / <xref:Bodu.Extensions.ComparableHelper> | Clamp, bounds, and comparison predicates over `IComparable<T>`, with nullable-aware helpers. |
| <xref:Bodu.Extensions.EnumExtensions> / <xref:Bodu.Extensions.Enums> | Flag manipulation, `[Description]` / `[Display]` text, and cached `GetValues` / `GetNames` / `TryParse`. |
| <xref:Bodu.Extensions.ArrayExtensions> / <xref:Bodu.Extensions.SpanExtensions> | Copy, slice, pad, reverse, clear, and jagged-to-rectangular conversion; read-only and reversed span views. |
| <xref:Bodu.Extensions.StreamExtensions> | `ReadAllBytes` / `WriteAllBytes` and their async forms. |
| <xref:Bodu.Extensions.BufferConverter> | Reinterpreting byte buffers as primitives and back, with endian swapping. |

Every example was run; the comments are the real outputs.

## Pattern 1 — `NumericExtensions`

```csharp
using Bodu.Extensions;

byte[] digits   = 90210u.ToDigitArray();                    // [9, 0, 2, 1, 0]
uint reversed   = 90210u.ReverseDigits();                   // 1209 — leading zero dropped
uint rotL       = 12345u.RotateDigitsLeft();                // 23451
uint rotR       = 12345u.RotateDigitsRight(2);              // 45123

bool p1 = 97.IsPrime();                                     // true
bool p2 = 91L.IsPrime();                                    // false (7 × 13)

int gcd    = 84.GreatestCommonDivisor(36);                  // 12
int gcdAll = new[] { 12, 18, 30 }.GreatestCommonDivisor();  // 6
int lcm    = 4.LeastCommonMultiple(6);                      // 12
long lcmAll = new long[] { 4, 6, 10 }.LeastCommonMultiple();// 60

uint bitsFlipped = 0b0000_0001u.ReverseBits();              // 0x80000000 — full 32-bit mirror
byte nibble      = ((byte)0b0110).ReverseBits(bitLength: 4);// 6 — mirror within the low 4 bits only
uint bytesSwapped = 0x12345678u.ReverseBytes();             // 0x78563412
uint wordsSwapped = 0x12345678u.ReverseWords();             // 0x34127856 — 16-bit halves exchanged
uint rotl = 0x80000001u.RotateBitsLeft(1);                  // 3
ushort rotr = ((ushort)1).RotateBitsRight(1);               // 32768

double sig  = 123456.789.RoundToSignificantDigits(4);       // 123500
decimal sigD = 0.00123456m.RoundToSignificantDigits(2);     // 0.0012

byte[] le = 0x0102u.GetBytes();                             // 02-01-00-00 — little-endian
byte[] be = 0x0102u.GetBytes(asBigEndian: true);            // 00-00-01-02
```

| Group | Members and widths |
|---|---|
| Digits | `ToDigitArray`, `ReverseDigits`, `RotateDigitsLeft`, `RotateDigitsRight` (+ `count` overloads) on `ushort`, `uint`, `ulong` |
| Number theory | `IsPrime` on `short` … `ulong`; `GreatestCommonDivisor` / `LeastCommonMultiple` as pairs and over arrays, `short` … `ulong` |
| Bits | `ReverseBits` (whole width, or the low `bitLength` bits) on `byte` … `ulong` and `byte[]`; `RotateBitsLeft` / `RotateBitsRight` on `byte` … `ulong` |
| Bytes and words | `ReverseBytes` on `ushort` … `ulong`; `ReverseWords` on `ushort` … `ulong`, `byte[]`, and `Span<byte>` (in place) |
| Rounding | `RoundToSignificantDigits(digits)` on `double` and `decimal` |
| Extraction | `GetBytes<T>(asBigEndian = false)` for any unmanaged `T` |

Rotation counts must lie in `[0, width]` (0 and the full width are no-ops; anything else throws `ArgumentOutOfRangeException`), `RoundToSignificantDigits` accepts 1–15 significant digits for `double` (`ArgumentOutOfRangeException` otherwise), and the array forms of `GreatestCommonDivisor` / `LeastCommonMultiple` reject an empty array.

## Pattern 2 — `ComparableExtensions` and `ComparableHelper`

The extension forms work on any `IComparable<T>` (with an `IComparer<T>` overload each); the bounds may be `null` to mean "unbounded" for reference types and, for `Clamp` on value types, `Nullable<T>`.

```csharp
using Bodu.Extensions;

int clamped  = 15.Clamp(0, 10);                  // 10
int floor    = (-3).AtLeast(0);                  // 0
int ceiling  = 42.AtMost(10);                    // 10
bool inside  = 5.IsBetween(1, 10);               // true — inclusive
bool reversed = 5.IsBetween(10, 1);              // true — bound order does not matter
bool outside = 15.IsOutside(1, 10);              // true
int upperOnly = 5.Clamp(min: null, max: 3);      // 3 — null bound = unbounded (value types use Nullable<T>)
bool strIn   = "m".IsBetween("a", "z");          // true
bool strNull = "m".IsBetween(null, "z");         // false — a null bound on a reference type never matches
bool greater = "b".IsGreaterThan("a");           // true
int max = 3.Max(7), min = 3.Min(7);              // 7, 3
bool sameCi  = "Apple".IsEqualTo("apple", StringComparer.OrdinalIgnoreCase);   // true

string? first = ComparableHelper.Coalesce<string>(null, "b");   // "b"
string? larger = ComparableHelper.Max<string>("a", null);       // "a" — null is ignored, not treated as smallest
bool bothNull = ComparableHelper.Min<string>(null, null) is null;   // true
```

| Member | Semantics |
|---|---|
| `Clamp(min, max)` | Returns `value` limited to `[min, max]`; `min > max` throws `ArgumentException`. |
| `AtLeast(min)` / `AtMost(max)` | One-sided clamps. |
| `IsBetween(a, b)` / `IsOutside(a, b)` | Inclusive membership in the range spanned by `a` and `b` in either order. |
| `IsLessThan`, `IsLessThanOrEqual`, `IsGreaterThan`, `IsGreaterThanOrEqual`, `IsEqualTo(other, comparer)` | Comparison predicates; `IsEqualTo` requires a comparer. |
| `Min(other)` / `Max(other)` | Pairwise. |
| `ComparableHelper.Min` / `Max` / `Coalesce` | Static, nullable-aware: a `null` operand is skipped and the other returned; both `null` gives `null`. |

## Pattern 3 — `EnumExtensions` and `Enums`

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Bodu.Extensions;

[Flags]
public enum Access
{
    None = 0,
    [Description("Read access")] [Display(Name = "Read")] Read = 1,
    [Description("Write access")] [Display(Name = "Write")] Write = 2,
    [Description("Execute access")] Execute = 4,
}
```

```csharp
using Bodu.Extensions;

Access access = Access.Read;
access = access.SetFlag(Access.Write);                       // Read, Write
bool both = access.HasAllFlags(Access.Read | Access.Write);  // true
bool any  = access.HasAnyFlag(Access.Execute);               // false
access = access.ClearFlag(Access.Read).ToggleFlag(Access.Execute);   // Write, Execute

string desc    = Access.Read.GetDescription();               // "Read access"   — [Description]
string display = Access.Read.GetDisplayName();               // "Read"          — [Display(Name)]
string fallback = Access.Execute.GetDisplayName();           // "Execute access" — falls back to [Description], then the name
bool hasDesc   = Access.None.TryGetDescription(out string d); // false; d == "None" (the member name)

Access[] all  = Enums.GetValues<Access>();                   // None, Read, Write, Execute — a copy of the cached array
string[] names = Enums.GetNames<Access>();
bool parsed   = Enums.TryParse("write", ignoreCase: true, out Access w);       // true, Write
bool unknown  = Enums.TryParse("Bogus", out Access _);                          // false
bool byDesc   = Enums.TryParseDescription("Execute access", out Access x);     // true, Execute
```

`SetFlag` / `ClearFlag` / `ToggleFlag` / `HasAllFlags` / `HasAnyFlag` work on any enum, converting through the underlying integer without boxing. The attribute readers are generic over `TEnum` and read the attributes once per enum type into a static cache; `GetValues` and `GetNames` return a fresh copy of their cached arrays on every call, so callers may mutate the result freely. `GetDisplayName` prefers `[Display(Name = …)]`, then `[Description]`, then the member name; `GetDescription` prefers `[Description]` and falls back to the name; `TryGetDescription` reports whether an attribute was actually found.

## Pattern 4 — `ArrayExtensions` and `SpanExtensions`

```csharp
using Bodu.Extensions;

int[] source = { 1, 2, 3, 4, 5 };

int[] middle   = source.Slice(1, 3);            // 2, 3, 4
int[] partRev  = source.Reverse(1, 3);          // 1, 4, 3, 2, 5 — a new array; source untouched
int[] rangeRev = source.Reverse(..2);           // 2, 1, 3, 4, 5
int[] padL     = source.PadLeft(7, 0);          // 0, 0, 1, 2, 3, 4, 5
int[] padR     = source.PadRight(6, 9);         // 1, 2, 3, 4, 5, 9
int[] copy     = source.Copy();
copy.Clear(1, 2);                               // 1, 0, 0, 4, 5 — in place, source untouched

int[,] matrix     = new[] { new[] { 1, 2, 3 }, new[] { 4, 5, 6 } }.ToMatrix(transpose: false);   // 2×3
int[,] transposed = new[] { new[] { 1, 2, 3 }, new[] { 4, 5, 6 } }.ToMatrix(transpose: true);    // 3×2; [2, 1] == 6

Span<int> span = stackalloc int[] { 1, 2, 3 };
ReadOnlySpan<int> readOnly = span.AsReadOnly();
Span<int> reversed = readOnly.ToReversed();     // new buffer: 3, 2, 1; span still 1, 2, 3
```

| Member | Notes |
|---|---|
| `Copy()` | Shallow copy. |
| `Slice(index[, count])` | New array. |
| `PadLeft(totalLength, padValue)` / `PadRight(totalLength, padValue)` | Returns the input unchanged when already long enough. |
| `Reverse()` / `Reverse(index, count)` / `Reverse(Range)` | **Returns a new array** — unlike `Array.Reverse`, which is in place. Non-generic `Array` overloads exist too. |
| `Clear()` / `Clear(index[, count])` | In place; generic and non-generic forms. |
| `ToMatrix(transpose)` | Jagged `T[][]` → rectangular `T[,]`; every row must have the same length. |
| `AsReadOnly()` | `Span<T>` → `ReadOnlySpan<T>` without a cast. |
| `ToReversed()` (+ `(index, count)` / `Range`) | Reversed **copy** into a new array-backed `Span<T>`; the source is not modified. |

## Pattern 5 — `StreamExtensions` and `BufferConverter`

```csharp
using Bodu.Extensions;

using var stream = new MemoryStream();
stream.WriteAllBytes(new byte[] { 0x78, 0x56, 0x34, 0x12, 0x01, 0x00 });
stream.Position = 0;
byte[] all = stream.ReadAllBytes();                        // 6 bytes — reads to the end from the current position

uint first    = all.Read<uint>(0);                         // 0x12345678 — little-endian reinterpretation
ushort second = all.Read<ushort>(4);                       // 1
ushort[] words = all.ToArray<ushort>(0, 3);                // 0x5678, 0x1234, 0x0001

byte[] target = new byte[4];
0xAABBCCDDu.CopyTo(target, 0);                             // DD-CC-BB-AA

Span<uint> swap = stackalloc uint[] { 0x11223344 };
swap.SwapEndian();                                         // 0x44332211 — in place
```

```csharp
using Bodu.Extensions;

using var stream = new MemoryStream();
await stream.WriteAllBytesAsync(new byte[] { 1, 2, 3 });
stream.Position = 0;
byte[] bytes = await stream.ReadAllBytesAsync();           // 3 bytes
```

| Member | Notes |
|---|---|
| `ReadAllBytes()` / `ReadAllBytesAsync(cancellationToken)` | Reads from the current position to the end and leaves the stream open. A seekable stream gets a single buffer sized to the remaining length; a non-seekable one is read in chunks. |
| `WriteAllBytes(ReadOnlySpan<byte>)` / `WriteAllBytesAsync(ReadOnlyMemory<byte>, cancellationToken)` | Writes the whole payload at the current position; the stream is neither flushed nor closed. |
| `Read<T>(index)` on `byte[]` / `Read<T>()` on `ReadOnlySpan<byte>` | Reinterprets the bytes at that position as an unmanaged `T` in the platform's byte order. |
| `ToArray<T>(sourceIndex, count)` | Reinterprets `count` consecutive `T` values. |
| `CopyTo<T>(…)` | Value → byte array, byte array → `T[]`, span → span, and memory → memory forms. |
| `SwapEndian<T>()` on `Span<T>` / `SwapEndian(source, destination, elementSize)` | Reverses the byte order of each element. |

`BufferConverter` uses the platform's native byte order (little-endian on every supported .NET target); combine with `SwapEndian` or `ReverseBytes` when a wire format is big-endian.

## Where to go next

- [String extensions](string-extensions.md) and [Date and time extensions](date-extensions.md) — the two large families in the same namespace.
- [Pooled buffer builder](pooled-buffer-builder.md) — `ArrayPool<T>`-backed accumulation when the byte payload is built incrementally.
- [Sequence operators and generators](sequence-operators.md) — the `IEnumerable<T>` / `IList<T>` / `IDictionary<TKey,TValue>` helpers.
- [`Bodu.Extensions` API reference](xref:Bodu.Extensions) — every overload with full signatures.
- **[Core Foundations guides](../topics/core-foundations.md)** — every guide in this topic.
