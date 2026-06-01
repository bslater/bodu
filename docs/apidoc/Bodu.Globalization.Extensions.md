---
uid: Bodu.Globalization.Extensions
---

![Bodu.Core](~/images/hero-core.svg)

## Purpose

**Bodu.Globalization.Extensions** holds the globalisation-specific helpers used by `Bodu.Globalization.Calendar` and surfaced for consumer use. The types here augment the BCL globalisation surface with calendar-aware helpers that don't fit naturally into <xref:Bodu.Extensions>.

## Key types

The namespace provides helpers for working with `CultureInfo`, `RegionInfo`, `CalendarWeekendDefinition`, and `WorkingDaysOfWeek` in scenarios where the BCL surface is too narrow for calendar-style queries.

For the broader date / time / culture surface, see <xref:Bodu.Extensions.DateTimeExtensions>, <xref:Bodu.Extensions.DateOnlyExtensions>, and <xref:Bodu.Globalization.Extensions.DateTimeFormatInfoExtensions>.

## Notes

- **Companion namespace.** Most date / time helpers live in <xref:Bodu.Extensions>; this namespace covers the narrower globalisation-aware surface that `Bodu.Globalization.Calendar` depends on.
- **See also:** the [Bodu.Core introduction](~/docs/core/index.md), the [Bodu.Globalization.Calendar introduction](~/docs/calendar/index.md), <xref:Bodu.Extensions>.
