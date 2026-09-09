---
title: Async resolution, localized names, and typed catalogues
---

# Async resolution, localized names, and typed catalogues

Four smaller surfaces of `Bodu.Globalization.Calendar` sit beside the main service and are easy to miss: streaming a wide range with `ResolveAsync`, presenting culture-specific display names with <xref:Bodu.Globalization.Calendar.NotableDateNameLocalizer>, loading the bundled catalogues by their strongly-typed <xref:Bodu.Globalization.Calendar.CommonNotableDateCatalog> names, and reading a loaded rule's duration and applicability model. This page covers each, in that order.

## Pattern 1 — stream a multi-year range with `ResolveAsync`

`NotableDateServiceAsyncExtensions.ResolveAsync(range, territory, filter = null, cancellationToken = default)` is an extension on <xref:Bodu.Globalization.Calendar.INotableDateService> that returns `IAsyncEnumerable<NotableDate>`. Resolution is synchronous, CPU-bound work with nothing to overlap, so the method does **not** offload to the thread pool; instead it resolves the range **one civil year at a time**, yields that year's occurrences, and then yields control (`Task.Yield`) before the next year. The consumer therefore sees progress — and cancellation — at year granularity instead of waiting for the whole decade.

<!-- compile -->
```csharp
INotableDateService service = AmericasCalendarData.CreateService("US");
var decade = new DateRange(new DateOnly(2020, 1, 1), new DateOnly(2029, 12, 31));

await foreach (NotableDate occurrence in service.ResolveAsync(decade, "US", cancellationToken: cancellationToken))
{
    Console.WriteLine($"{occurrence.Date:yyyy-MM-dd} {occurrence.DisplayName}");
}
```

The streamed sequence is element-for-element identical to `service.Resolve(range, territory)` — the same occurrences, in the same date-then-identity order, with a multi-day occurrence that spans a year boundary yielded exactly once. Pass a <xref:Bodu.Globalization.Calendar.NotableDateFilter> to restrict it exactly as the synchronous overload would.

Two contract details:

- **Arguments are validated eagerly**, when `ResolveAsync` is called (`ArgumentNullException` for a `null` service or territory, `ArgumentOutOfRangeException` for an inverted range); resolution starts on the first `MoveNextAsync`.
- **Cancellation is observed between years.** The token — passed directly or through `WithCancellation` — is checked before each year resolves, so a request over a single year cancels only before or after that year; an <xref:System.OperationCanceledException> surfaces from the enumeration.

Over the caching decorator the same extension applies unchanged: each year is one cache lookup, which is exactly the cache's unit.

## Pattern 2 — localized display names

Resolution is culture-agnostic: every <xref:Bodu.Globalization.Calendar.NotableDate> carries the invariant `DisplayName` authored in the resource. Localization is applied *afterwards* to resolved occurrences through <xref:Bodu.Globalization.Calendar.INotableDateNameLocalizer>, so a single service can answer any culture. `NotableDateNameLocalizer` is the dictionary-backed implementation:

```csharp
using System.Globalization;
using Bodu.Globalization.Calendar;

var names = new NotableDateNameLocalizer()
    .Register("new-years-day", CultureInfo.GetCultureInfo("fr"), "Jour de l'An")
    .Register("new-years-day", CultureInfo.GetCultureInfo("de-AT"), "Neujahrstag")
    .Register("new-years-day", CultureInfo.InvariantCulture, "New Year");

INotableDateService service = AmericasCalendarData.CreateService("US");
IReadOnlyList<NotableDate> january1 = service.Resolve(new DateOnly(2026, 1, 1), "US");
NotableDate newYear = january1[0];

string? frCa = names.GetDisplayName(newYear, CultureInfo.GetCultureInfo("fr-CA"));   // "Jour de l'An" — fr-CA → fr
string? deAt = names.GetDisplayName(newYear, CultureInfo.GetCultureInfo("de-AT"));   // "Neujahrstag"  — exact match
string? deDe = names.GetDisplayName(newYear, CultureInfo.GetCultureInfo("de-DE"));   // "New Year"     — de-DE → de → invariant
string? none = new NotableDateNameLocalizer().GetDisplayName(newYear, CultureInfo.GetCultureInfo("fr"));   // null — nothing registered
```

**How names resolve.** `Register(notableDateId, culture, displayName)` stores a name against the *concept id* (`NotableDate.NotableDateId`, the `id` of the `<NotableDate>` element — `new-years-day` above) and the culture's `Name`; a later registration for the same pair replaces the earlier one, and the method returns the localizer for chaining. `GetDisplayName` walks the requested culture's parent chain — `fr-CA` → `fr` → invariant — and returns the first registered name, or `null` when the chain is exhausted. Registering against `CultureInfo.InvariantCulture` therefore supplies the default that every culture falls back to. Lookups are keyed by concept id only, so one registration covers every rule and territory that emits that concept.

Apply a localizer to results with the `Localize` extensions on <xref:Bodu.Globalization.Calendar.NotableDateLocalizationExtensions>, which return copies whose `DisplayName` is replaced when the localizer answers and left unchanged when it returns `null`:

```csharp
using System.Globalization;
using Bodu.Globalization.Calendar;

var names = new NotableDateNameLocalizer()
    .Register("new-years-day", CultureInfo.GetCultureInfo("fr"), "Jour de l'An");

INotableDateService service = AmericasCalendarData.CreateService("US");
IReadOnlyList<NotableDate> january1 = service.Resolve(new DateOnly(2026, 1, 1), "US");

NotableDate french = january1[0].Localize(names, CultureInfo.GetCultureInfo("fr-FR"));            // DisplayName "Jour de l'An"
IReadOnlyList<NotableDate> japanese = january1.Localize(names, CultureInfo.GetCultureInfo("ja"));  // DisplayName unchanged
```

For names sourced from `.resx` files or a database, implement `INotableDateNameLocalizer` yourself — it is a single method — as [Building and extending the service](building-the-service.md#localizing-display-names) shows.

## Pattern 3 — typed access to the bundled catalogues

The runtime embeds the shared catalogues that territory packs import — `global-core`, `christian-western`, `global-islamic`, and so on. <xref:Bodu.Globalization.Calendar.CommonNotableDateResources> exposes them three ways, and <xref:Bodu.Globalization.Calendar.CommonNotableDateCatalog> gives each a strongly-typed name so the raw string never has to be spelled:

| Member | Returns | Use it when |
|---|---|---|
| `GetResourceName(CommonNotableDateCatalog)` | the bare kebab-case name (`ChristianWestern` → `christian-western`) | you are writing an `Import` directive or building a resolver by name |
| `Resolve(string)` / `Resolve(CommonNotableDateCatalog)` | the catalogue's raw XML, or `null` when not bundled | you need the document text — to inspect, embed, or hand to your own loader |
| `Resolver` | a `Func<string, string?>` over `Resolve(string)` | you are loading a territory document whose imports must resolve against the bundled catalogues |
| `Load(CommonNotableDateCatalog)` | a validated <xref:Bodu.Globalization.Calendar.NotableDateResource> | you want to use a catalogue directly as a service's resource |

```csharp
using Bodu.Globalization.Calendar;

string name = CommonNotableDateResources.GetResourceName(CommonNotableDateCatalog.ChristianWestern);   // "christian-western"
string? xml = CommonNotableDateResources.Resolve(CommonNotableDateCatalog.GlobalCore);                   // the catalogue's XML

// Materialize a catalogue as a resource and query it directly.
NotableDateResource western = CommonNotableDateResources.Load(CommonNotableDateCatalog.ChristianWestern);   // resourceId "common.christian-western"
var service = new NotableDateService(western);
IReadOnlyList<NotableDate> easter = service.Resolve(new DateOnly(2026, 4, 5), "US");   // easter-sunday

// Load a territory document of your own whose <Use> imports name the bundled catalogues.
string myDocumentXml = File.ReadAllText("rules/holidays.xml");
NotableDateResource mine = NotableDateResourceLoader.Load(myDocumentXml, CommonNotableDateResources.Resolver);
```

`Load` caches the materialized resource per catalogue — a `NotableDateResource` is immutable, so repeated calls return the **same instance** and the parse-and-validate cost is paid once per process. Every member that takes the enum throws <xref:System.ArgumentOutOfRangeException> for an undefined value, and `Load` throws <xref:System.InvalidOperationException> if the catalogue's content is not bundled with the assembly. Catalogue resources declare no territories of their own — rules in them are global and apply to any territory you query with — which is why the territory packs import them and add scope rather than the reverse. The full list of members and what each catalogue contains is in the [notable-date catalogue](catalogue/index.md).

## Pattern 4 — reading a rule's duration and applicability

A loaded <xref:Bodu.Globalization.Calendar.NotableDateRule> carries two value objects that the resolution pipeline consults and that are useful to inspect when you generate rules or explain results:

**`Duration`** is a <xref:Bodu.Globalization.Calendar.NotableDateDurationDefinition>, or `null` when the rule inherits the concept's `DefaultDurationDays`. It is an abstract record with exactly two concrete kinds, mutually exclusive by construction:

- <xref:Bodu.Globalization.Calendar.FixedDurationDefinition>`(int Days)` — a fixed span of calendar days including the start; the resolved end date is `Date + Days − 1`, and values below one are clamped to one at resolution. Authored as the `durationDays` attribute.
- <xref:Bodu.Globalization.Calendar.CalculatedEndDateDurationDefinition> — a span whose end is computed by a second strategy (`<Duration><UntilDate>`), so it can vary in length and cross a year boundary. See [NotableDateRule and adjustment-policy reference](rule-reference.md).

**`Applicability`** is a <xref:Bodu.Globalization.Calendar.RuleApplicability> describing *where and when* the rule applies: `Calendar` (the calendar system the strategy is expressed in), optional `FromYear` / `ToYear` bounds, the explicit `Territories` list, `OnlyYears` / `ExceptYears`, and the `EveryYears` cadence with its `AnchorYear`. Three methods answer the questions the pipeline asks:

| Method | Answers |
|---|---|
| `AppliesTo(territory, year)` | Does the rule apply to this territory in this year — territory match **and** every year constraint. |
| `MatchesTerritory(territory)` | Does the rule's territory list include the territory or one of its parents (a rule scoped to `US` matches a query for `US-CA`; a rule scoped to `US-CA` does not match `US`). A rule with no territories is global and matches everything. |
| `MatchSpecificity(territory)` | The length of the most specific matching code (`2` for `US`, `5` for `US-CA`), `0` for a global rule, `-1` for no match — the number the pipeline uses to let a narrower same-concept rule shadow a broader one. |

```csharp
using Bodu.Globalization.Calendar;

NotableDateResource us = AmericasCalendarData.LoadResource("US");

// Inauguration Day is authored for DC only, every fourth year anchored on 1937.
NotableDateRule inauguration = us.NotableDates
    .SelectMany(definition => definition.Rules)
    .First(rule => rule.Applicability.EveryYears is not null);

RuleApplicability when = inauguration.Applicability;
int? every  = when.EveryYears;                         // 4
int? anchor = when.AnchorYear;                         // 1937
bool in2025 = when.AppliesTo("US-DC", 2025);           // true
bool in2026 = when.AppliesTo("US-DC", 2026);           // false — off-cadence year

NotableDateRule newYear = us.NotableDates.First(d => d.Id == "new-years-day").Rules[0];
bool national   = newYear.Applicability.MatchesTerritory("US-CA");    // true — US is a parent of US-CA
int specificity = newYear.Applicability.MatchSpecificity("US-CA");    // 2 — matched via "US"
int noMatch     = newYear.Applicability.MatchSpecificity("CA");       // -1

NotableDateDurationDefinition? duration = newYear.Duration;           // null — inherits DefaultDurationDays (1)
```

Applicability is an input to the pipeline, not the whole story: an occurrence can still be suppressed by an override, a collision, or an adjustment policy. [The resolution pipeline](resolution-pipeline.md) and [Rule identity, priority, and observed-date resolution](identity-and-resolution.md) describe those later stages.

## Where to go next

- **[Using NotableDateService](notable-dates.md)** — the synchronous resolution surface `ResolveAsync` mirrors.
- **[Building and extending the service](building-the-service.md)** — implementing `INotableDateNameLocalizer` over resources, and the other collaborators.
- **[Authoring notable date rules](rule-authoring.md)** — the `<Use>` imports that `CommonNotableDateResources.Resolver` satisfies.
- **[Notable-date catalogue](catalogue/index.md)** — what each bundled catalogue contains.
- **[Bodu.Globalization.Calendar API reference](xref:Bodu.Globalization.Calendar)** — `NotableDateServiceAsyncExtensions`, `NotableDateNameLocalizer`, `CommonNotableDateResources`, `RuleApplicability`.
- **[Globalization & Calendars guides](../topics/globalization-and-calendars.md)** — every guide in this topic: the runtime, companions, data packs, and the notable-date catalogue.
