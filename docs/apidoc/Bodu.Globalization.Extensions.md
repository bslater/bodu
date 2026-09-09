---
uid: Bodu.Globalization.Extensions
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

**Bodu.Globalization.Extensions** is a small companion namespace in the `Bodu.Core` package holding a single extension class over the BCL <xref:System.Globalization.DateTimeFormatInfo>. It answers one question the BCL leaves open — a culture declares the day its week *starts* on (`FirstDayOfWeek`), but not the day it *ends* on — and the week-boundary helpers in <xref:Bodu.Extensions> build on it.

## Key types

- <xref:Bodu.Globalization.Extensions.DateTimeFormatInfoExtensions> — one method, `LastDayOfWeek(this DateTimeFormatInfo)`, which returns the day six days after the culture's `FirstDayOfWeek`: Sunday for a Monday-first culture, Saturday for a Sunday-first one.

## Example

```csharp
using System.Globalization;
using Bodu.Globalization.Extensions;

DateTimeFormatInfo enGb = CultureInfo.GetCultureInfo("en-GB").DateTimeFormat;
DayOfWeek first = enGb.FirstDayOfWeek;    // Monday — BCL property
DayOfWeek last  = enGb.LastDayOfWeek();   // Sunday — Bodu extension
```

## Notes

- **Companion namespace.** The broader date / time / culture surface — first and last date of the week, ISO week numbering, working-week tests — lives in <xref:Bodu.Extensions.DateTimeExtensions> and <xref:Bodu.Extensions.DateOnlyExtensions>; day-of-week *sets* are modelled by <xref:Bodu.WeekPattern> and <xref:Bodu.WorkingDaysOfWeek>.
- **See also:** the [Bodu.Core introduction](~/docs/core/index.md), <xref:Bodu.Extensions>.
