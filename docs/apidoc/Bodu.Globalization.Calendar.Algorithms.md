---
uid: Bodu.Globalization.Calendar.Algorithms
---

![Bodu.Globalization.Calendar](~/images/hero-calendar.svg)

## Purpose

**Bodu.Globalization.Calendar.Algorithms** ships the pluggable date-resolution algorithms for moveable observances that cannot be expressed as fixed dates or static day-of-week patterns. Each algorithm implements <xref:Bodu.Globalization.Calendar.INotableDateAlgorithm> and is registered on an <xref:Bodu.Globalization.Calendar.INotableDateAlgorithmRegistry> under a stable string key referenced by `NotableDateRule.AlgorithmKey`.

Reach for this namespace when an authored rule needs Easter, Vesak, Asalha Puja, Losar, Qingming, or a Hindu lunar festival, or when you are writing a custom algorithm that follows the same contract.

## Static documentation

- **[Date calculation algorithms guide](~/guides/calendar/algorithms.md)** — the full per-algorithm walkthrough.
- **[`Bodu.Globalization.Calendar` introduction](~/docs/calendar/index.md)** — how algorithms fit into the resolution pipeline.

## Key types

**Easter algorithms**

- <xref:Bodu.Globalization.Calendar.Algorithms.EasterSundayNotableDateAlgorithm> — Computus-based Gregorian Easter Sunday for years ≥ 1583 and Meeus's Julian adaptation for earlier years. Results cached per `(year, calendar)`. Algorithm key: `"easter"`.
- <xref:Bodu.Globalization.Calendar.Algorithms.GregorianEasterSundayNotableDateProvider> — Gregorian-only provider variant; accepts `null` or `GregorianCalendar`.
- <xref:Bodu.Globalization.Calendar.Algorithms.OrthodoxEasterSundayNotableDateProvider> — Orthodox (Julian computus) Easter, returned as the Gregorian-equivalent `DateTime`. Accepts `null` or `JulianCalendar`.
- <xref:Bodu.Globalization.Calendar.Algorithms.EasterSundayNotableDateProviderBase> — abstract base for Easter providers with per-year caching via a `ConcurrentDictionary`. Derive when you need a custom Easter variant.

**Lunar / solar-term algorithms**

- <xref:Bodu.Globalization.Calendar.Algorithms.VesakNotableDateAlgorithm> — the first full moon on or after 1 May. Accurate within one day for 1900–2100. Uses Meeus Chapter 49 lunar-phase computation.
- <xref:Bodu.Globalization.Calendar.Algorithms.AsalhaPujaNotableDateAlgorithm> — the first full moon on or after 15 June (Theravada Dharma Day; start of Vassa).
- <xref:Bodu.Globalization.Calendar.Algorithms.LosarNotableDateAlgorithm> — Tibetan New Year, approximated as the first new moon on or after 20 January using the Chinese lunisolar approximation. Diverges from the official Tibetan calendar approximately every 3–5 years by ~1 month; register a custom algorithm using TMAI tables when exact dates are required.
- <xref:Bodu.Globalization.Calendar.Algorithms.QingmingNotableDateAlgorithm> — the Qingming solar term (清明節), when the sun's ecliptic longitude reaches 15° (typically 4–5 April). Accurate within one day for 1901–2100.

**Hindu lunisolar**

- <xref:Bodu.Globalization.Calendar.Algorithms.HinduLunarNotableDateAlgorithm> — Hindu festival dates from (month, paksha, tithi) lunisolar coordinates. Constructor: `(HinduLunarMonth month, HinduPaksha paksha, int tithi)`. Uses fixed month-offset approximation with ~19-year Metonic-cycle correction; accurate within 1–2 days for 1900–2100.
- <xref:Bodu.Globalization.Calendar.Algorithms.HinduLunarMonth> — `Chaitra`, `Vaisakha`, `Jyaistha`, `Asadha`, `Sravana`, `Bhadrapada`, `Asvina`, `Kartika`, `Margasirsa`, `Pausa`, `Magha`, `Phalguna`.
- <xref:Bodu.Globalization.Calendar.Algorithms.HinduPaksha> — `Shukla` (bright fortnight) or `Krishna` (dark fortnight).

## Example

```csharp
using Bodu.Globalization.Calendar;

// Register the shipped algorithms.
var registry = new NotableDateAlgorithmRegistry();
registry.Register("easter",         new EasterSundayNotableDateAlgorithm());
registry.Register("vesak",          new VesakNotableDateAlgorithm());
registry.Register("qingming",       new QingmingNotableDateAlgorithm());
registry.Register("diwali",         new HinduLunarNotableDateAlgorithm(
                                        HinduLunarMonth.Kartika, HinduPaksha.Krishna, tithi: 15));

// A rule that uses one.
var rule = new NotableDateRule
{
    Name         = "Easter Sunday",
    Strategy     = DateResolutionStrategy.Algorithm,
    AlgorithmKey = "easter",
    Category     = NotableDateCategory.Religious,
};
```

## Notes

- **Caching.** Every shipped algorithm caches per `(year, calendar)` in a process-wide `ConcurrentDictionary` — a rule referenced from many territories pays the computation cost once per year.
- **Accuracy windows.** Lunar and solar-term algorithms approximate astronomical events. Each algorithm's `<remarks>` documents its accuracy window. For applications that require official astronomical positions outside the documented window, supply a custom `INotableDateAlgorithm`.
- **Algorithm key vs. type.** Rules may reference algorithms by `AlgorithmKey` (preferred — decoupled from assembly type names) or by `AlgorithmType` (assembly-qualified type name, reflection-activated fallback).
- **See also:** the [Date calculation algorithms guide](~/guides/calendar/algorithms.md), the [non-Gregorian calendars guide](~/guides/calendar/non-gregorian-calendars.md), the [`NotableDateRule` reference](~/guides/calendar/rule-reference.md).
