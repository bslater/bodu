# Calendar Notable Date Cookbook Schema Strategy

## 1. Purpose

This document proposes a clean, forward-looking design strategy for the Bodu Calendar notable-date cookbook schema. It builds on the existing XML cookbook structure while addressing the principal design risks identified in the current implementation: ambiguous rule identity, name-based override targeting, unclear priority behaviour, and observed-date adjustment behaviour that can be difficult to reason about.

The proposed design keeps the useful concepts already present in the current schema, including:

- resource-level imports;
- declarative notable-date rules;
- fixed-date, weekday-based, offset, relative weekday, and algorithm strategies;
- territory and calendar scoping;
- tags and categories;
- adjustment logic;
- cookbook-style reuse and override.

The recommended change is to formalise identity and precedence around explicit IDs rather than display names.

## 2. Design goals

The schema should support the following goals.

| Goal | Description |
|---|---|
| Stable identity | A notable date, a rule, and an adjustment policy must each have stable IDs. Display names must not be used as identity. |
| Multiple rules per notable date | A single notable-date concept must support multiple calculation rules for different territories, eras, calendars, or variants. |
| Deterministic override behaviour | Imports and overrides must target explicit IDs and produce predictable results. |
| First-class adjustment logic | Observed-date, substitution, suppression, and additional-observance behaviour must be explicit. |
| Clear priority semantics | Priority must have one documented meaning across rules, adjustments, overrides, and collisions. |
| Schema/runtime alignment | XSD, parser, runtime model, and documentation must describe the same authoring contract. |
| Safe period queries | A single-day query and a period query must not return contradictory actual-versus-observed results. |
| Validation before execution | Ambiguous anchors, duplicate IDs, malformed territories, invalid calendar/month combinations, and missing algorithms should be detected before date resolution. |

## 3. Core design principle

The central design principle is:

```text
A notable date is the concept.
A rule is one way of calculating that concept.
An adjustment policy describes how calculated occurrences are transformed, supplemented, or suppressed.
An override is a targeted patch against stable IDs.
```

Therefore:

```text
resourceId + notableDate.id + rule.id
```

is the full identity of a rule.

Names such as `New Year's Day`, `ANZAC Day`, and `Constitution Day` are presentation values. They are not lookup keys, merge keys, or override keys.

## 4. Recommended resource structure

The recommended top-level XML structure is:

```xml
<NotableDateResource schemaVersion="1.0" resourceId="boduglobal.au">
  <Metadata />
  <Imports />
  <ResolutionPolicy />
  <AdjustmentPolicies />
  <NotableDates />
  <Overrides />
</NotableDateResource>
```

Conceptually, this maps to:

```text
NotableDateResource
 ├─ Metadata
 ├─ Imports
 ├─ ResolutionPolicy
 ├─ AdjustmentPolicies
 ├─ NotableDates
 │   └─ NotableDate
 │       ├─ id
 │       ├─ displayName
 │       ├─ category
 │       ├─ defaults
 │       ├─ tags
 │       └─ Rules
 │           └─ Rule
 │               ├─ id
 │               ├─ priority
 │               ├─ Applicability
 │               ├─ Strategy
 │               └─ Adjustments
 └─ Overrides
     └─ explicit patch operations
```

This keeps the cookbook readable while making runtime resolution safe and deterministic.

## 5. Notable date model

A `NotableDate` represents the concept being described.

Recommended fields:

| Field | Purpose |
|---|---|
| `id` | Stable machine identity. Required. |
| `displayName` | Human-readable name. Required. |
| `category` | Default category for child rules. |
| `defaultDurationDays` | Default duration for rules that do not specify their own duration. |
| `defaultNonWorkingDay` | Default non-working-day flag for child rules. |
| `tags` | Optional tags inherited by rules unless overridden. |
| `Rules` | One or more calculation rules. |

Example:

```xml
<NotableDate
  id="new-years-day"
  displayName="New Year's Day"
  category="Holiday"
  defaultDurationDays="1"
  defaultNonWorkingDay="true">
  <Tags>
    <Tag value="civil" />
    <Tag value="new-year" />
  </Tags>
  <Rules>
    <!-- rules go here -->
  </Rules>
</NotableDate>
```

## 6. Rule model

A `Rule` represents one way of calculating a notable date.

Recommended fields:

| Field | Purpose |
|---|---|
| `id` | Stable rule identity within the parent notable date. Required. |
| `priority` | Rule precedence. Higher value should win unless a policy says otherwise. |
| `category` | Optional override of the parent notable-date category. |
| `nonWorking` | Optional override of the parent non-working-day flag. |
| `durationDays` | Optional override of the parent duration. |
| `Applicability` | Territory, calendar, and year applicability. |
| `Strategy` | Exactly one calculation strategy. |
| `Adjustments` | References to reusable policies or inline adjustment policies. |
| `Tags` | Rule-specific tags. |

Example:

```xml
<Rule id="gregorian-fixed-jan-1" priority="100">
  <Applicability calendar="Gregorian" fromYear="1901">
    <Territory code="AU" />
  </Applicability>

  <Strategy>
    <Fixed month="January" day="1" />
  </Strategy>

  <Adjustments>
    <Adjustment policyRef="au-weekend-public-holiday-observed" />
  </Adjustments>
</Rule>
```

## 7. Applicability model

Applicability should be represented as a dedicated block rather than a comma-separated string attribute.

```xml
<Applicability calendar="Gregorian" fromYear="1901" toYear="">
  <Territory code="AU" />
  <Territory code="AU-NSW" />
  <OnlyYear value="2020" />
  <ExceptYear value="2021" />
</Applicability>
```

Recommended behaviour:

| Element/attribute | Behaviour |
|---|---|
| `calendar` | Calendar system used by the strategy. Defaults to Gregorian when omitted. |
| `fromYear` | First valid civil/Gregorian year for generated occurrences. |
| `toYear` | Last valid civil/Gregorian year for generated occurrences. |
| `Territory` | One or more explicit territory elements. No comma-delimited strings. |
| `OnlyYear` | Optional explicit inclusion years. |
| `ExceptYear` | Optional explicit exclusion years. |

Territory values should remain ISO-style values such as:

```text
AU
AU-NSW
NZ
US
US-PR
```

## 8. Strategy model

The existing strategy catalogue is broadly sound and should be retained, but moved under a single `Strategy` wrapper for clearer XSD modelling and parser logic.

Recommended strategy elements:

| Strategy | Purpose |
|---|---|
| `Fixed` | Fixed month/day in the selected calendar. |
| `DayOfWeekInMonth` | Nth weekday in a month. |
| `WeekdayNearDate` | Weekday on/before/after/nearest a fixed date. |
| `RelativeWeekdayInMonth` | Weekday relative to another weekday anchor in the month. |
| `OffsetFromRule` | Offset from another explicitly referenced rule. |
| `Algorithm` | Algorithm-backed date calculation. |

### 8.1 Fixed

```xml
<Fixed month="April" day="25" />
```

For non-Gregorian calendars:

```xml
<Fixed calendarMonth="Nisan" day="15" />
```

The runtime validator should reject calendar/month combinations that are not supported by the selected calendar.

### 8.2 DayOfWeekInMonth

```xml
<DayOfWeekInMonth month="June" dayOfWeek="Monday" weekOrdinal="Second" />
```

### 8.3 WeekdayNearDate

```xml
<WeekdayNearDate month="June" day="20" dayOfWeek="Saturday" direction="OnOrAfter" />
```

Recommended `direction` values:

```text
Before
OnOrBefore
Nearest
OnOrAfter
After
```

The current schema has `OnOrBefore`, `OnOrAfter`, and `Nearest`. Adding strict `Before` and `After` avoids overloading boundary behaviour.

### 8.4 RelativeWeekdayInMonth

```xml
<RelativeWeekdayInMonth
  month="November"
  dayOfWeek="Monday"
  weekOrdinal="First"
  relativeDayOfWeek="Tuesday"
  direction="After" />
```

This supports rules such as United States federal general election day: the Tuesday after the first Monday in November.

### 8.5 OffsetFromRule

References must use stable IDs.

```xml
<OffsetFromRule notableDateRef="easter-sunday" ruleRef="western-gregorian" offsetDays="-2" />
```

If `ruleRef` is omitted and more than one rule exists for the referenced notable date, validation should fail as ambiguous.

### 8.6 Algorithm

```xml
<Algorithm key="western-easter" />
```

Optional parameters should be expressed as child elements rather than ad hoc attributes:

```xml
<Algorithm key="custom-lunar-festival">
  <Parameter name="month" value="Tishri" />
  <Parameter name="day" value="1" />
</Algorithm>
```

Missing algorithm handlers should be validation diagnostics, not silent missing notable dates.

## 9. Adjustment policy model

Adjustment logic should be first-class and reusable.

Recommended structure:

```xml
<AdjustmentPolicy id="au-weekend-public-holiday-observed" priority="100">
  <Scope>
    <Territory code="AU" />
    <Category value="Holiday" />
  </Scope>
  <Trigger type="FallsOn">
    <Weekday value="Saturday" />
    <Weekday value="Sunday" />
  </Trigger>
  <Action
    type="MoveToNextWorkingDay"
    skipWeekends="true"
    skipNonWorkingDates="true"
    maxSearchDays="7" />
  <Emission mode="ObservedOnly" reason="Observed public holiday" />
</AdjustmentPolicy>
```

The existing schema currently models adjustments as rule-local attributes such as `when`, `action`, `dayOfWeek`, `days`, and `target`. That should still be supported for simple inline adjustments, but the preferred structure should be reusable named policies.

## 10. Adjustment scope

Adjustment scope controls where the policy can apply.

```xml
<Scope>
  <Territory code="AU" />
  <Territory code="NZ" />
  <Category value="Holiday" />
  <NotableDate ref="new-years-day" />
</Scope>
```

Recommended scope dimensions:

| Scope item | Purpose |
|---|---|
| `Territory` | Limits the adjustment to specified territories. |
| `Calendar` | Limits the adjustment to a calendar system. |
| `Category` | Limits the adjustment to categories such as `Holiday`. |
| `NotableDate` | Limits the adjustment to specific notable-date concepts. |
| `Rule` | Limits the adjustment to a specific rule. |

A territory-scoped adjustment should not silently apply to a global rule unless the policy explicitly says so.

## 11. Adjustment trigger

Recommended trigger types:

```text
Always
FallsOn
IfWeekend
IfWeekday
IfNonWorkingDay
IfWorkingDay
IfBeforeFixedDate
IfAfterFixedDate
IfNthOccurrenceInMonth
CollidesWith
Custom
```

Example:

```xml
<Trigger type="FallsOn">
  <Weekday value="Saturday" />
  <Weekday value="Sunday" />
</Trigger>
```

Collision trigger example:

```xml
<Trigger type="CollidesWith">
  <Target category="Holiday" />
</Trigger>
```

## 12. Adjustment action

Recommended action types:

```text
None
AddDays
MoveToNextWeekday
MoveToPreviousWeekday
MoveToNextWorkingDay
MoveToPreviousWorkingDay
ReplaceWithRule
Suppress
AddObservedOccurrence
Custom
```

The current `MoveToNextNonWorkingDay` naming should be reconsidered. If the intent is to skip non-working days and land on a working day, the action should be called `MoveToNextWorkingDay`.

Examples:

```xml
<Action type="AddDays" days="1" />
```

```xml
<Action type="MoveToNextWorkingDay" maxSearchDays="7" />
```

```xml
<Action type="ReplaceWithRule" notableDateRef="boxing-day" ruleRef="gregorian-fixed-dec-26" />
```

## 13. Adjustment emission policy

Observed-date behaviour must be explicit.

```xml
<Emission mode="ObservedOnly" reason="Observed public holiday" />
```

Recommended emission modes:

| Mode | Meaning |
|---|---|
| `ActualOnly` | Emit only the original calculated occurrence. |
| `ObservedOnly` | Suppress the actual occurrence and emit only the adjusted occurrence. |
| `ActualAndObserved` | Emit both actual and adjusted occurrences. |
| `ObservedAsAdditional` | Emit the original date normally and emit the adjusted occurrence as an additional observance. |
| `Suppress` | Emit neither occurrence when the adjustment applies. |

This is essential to avoid a range query and a single-day query producing different actual-versus-observed answers.

## 14. Priority and conflict strategy

Priority should have a single meaning.

Recommended policy:

```text
Higher numeric priority wins.
If priority is equal, the more specific scope wins.
If scope is equal, later explicit override wins.
If still equal, stable IDs provide deterministic ordering only, not semantic priority.
```

Priority should apply consistently to:

- rule selection;
- adjustment selection;
- override application;
- duplicate resolution;
- same-day collision resolution where configured.

A suggested resource-level policy is:

```xml
<ResolutionPolicy
  duplicatePolicy="Error"
  sameDayCollisionPolicy="KeepAll"
  spanCollisionPolicy="KeepAll"
  priorityDirection="HigherWins"
  observedDateRangePolicy="ObservedOccurrenceControlsInclusion" />
```

Recommended policies:

| Policy | Recommended default | Reason |
|---|---|---|
| `duplicatePolicy` | `Error` | Avoid hidden rule collapse. |
| `sameDayCollisionPolicy` | `KeepAll` | Multiple notable dates can legitimately occur on the same day. |
| `spanCollisionPolicy` | `KeepAll` | Multi-day events and one-day events may overlap legitimately. |
| `priorityDirection` | `HigherWins` | Easy to understand and consistent with ranking. |
| `observedDateRangePolicy` | `ObservedOccurrenceControlsInclusion` | Prevent inconsistent single-day versus period answers. |

## 15. Imports and overrides

The current `UseFrom` model is valuable and should be retained, but the selection keys should move from display names to stable IDs.

Recommended import structure:

```xml
<Imports>
  <Import resource="boduglobal.common">
    <Include notableDateRef="new-years-day" />
    <Include notableDateRef="christmas-day" />
  </Import>
</Imports>
```

Import all:

```xml
<Import resource="boduglobal.common">
  <IncludeAll />
</Import>
```

Patch during import:

```xml
<Import resource="boduglobal.common">
  <Include notableDateRef="new-years-day">
    <PatchRule ruleRef="gregorian-fixed-jan-1" priority="200">
      <Applicability calendar="Gregorian">
        <Territory code="AU-NSW" />
      </Applicability>
    </PatchRule>
  </Include>
</Import>
```

Runtime/resource overrides should use operation elements:

```xml
<Overrides>
  <RemoveRule notableDateRef="labour-day" ruleRef="first-monday-october" />
  <PatchRule notableDateRef="new-years-day" ruleRef="gregorian-fixed-jan-1" priority="200" />
</Overrides>
```

Recommended override operations:

| Operation | Purpose |
|---|---|
| `AddNotableDate` | Add a new notable-date concept. |
| `PatchNotableDate` | Patch metadata for a concept. |
| `RemoveNotableDate` | Remove a concept and all rules. |
| `AddRule` | Add a rule to an existing concept. |
| `PatchRule` | Patch an existing rule. |
| `ReplaceRule` | Replace a rule completely. |
| `RemoveRule` | Remove a specific rule. |
| `AddAdjustmentPolicy` | Add a reusable policy. |
| `PatchAdjustmentPolicy` | Patch a reusable policy. |
| `RemoveAdjustmentPolicy` | Remove a reusable policy. |

## 16. Example 1 — New Year's Day with weekend observed-date policy

This example demonstrates:

- one notable-date concept;
- one fixed-date rule;
- reusable weekend observed-date policy;
- explicit `ObservedOnly` emission.

```xml
<NotableDateResource schemaVersion="1.0" resourceId="boduglobal.au">
  <ResolutionPolicy
    duplicatePolicy="Error"
    sameDayCollisionPolicy="KeepAll"
    spanCollisionPolicy="KeepAll"
    priorityDirection="HigherWins"
    observedDateRangePolicy="ObservedOccurrenceControlsInclusion" />

  <AdjustmentPolicies>
    <AdjustmentPolicy id="au-weekend-public-holiday-observed" priority="100">
      <Scope>
        <Territory code="AU" />
        <Category value="Holiday" />
      </Scope>
      <Trigger type="FallsOn">
        <Weekday value="Saturday" />
        <Weekday value="Sunday" />
      </Trigger>
      <Action
        type="MoveToNextWorkingDay"
        skipWeekends="true"
        skipNonWorkingDates="true"
        maxSearchDays="7" />
      <Emission mode="ObservedOnly" reason="Observed public holiday" />
    </AdjustmentPolicy>
  </AdjustmentPolicies>

  <NotableDates>
    <NotableDate
      id="new-years-day"
      displayName="New Year's Day"
      category="Holiday"
      defaultDurationDays="1"
      defaultNonWorkingDay="true">
      <Tags>
        <Tag value="civil" />
        <Tag value="new-year" />
      </Tags>
      <Rules>
        <Rule id="gregorian-fixed-jan-1" priority="100">
          <Applicability calendar="Gregorian" fromYear="1901">
            <Territory code="AU" />
          </Applicability>
          <Strategy>
            <Fixed month="January" day="1" />
          </Strategy>
          <Adjustments>
            <Adjustment policyRef="au-weekend-public-holiday-observed" />
          </Adjustments>
        </Rule>
      </Rules>
    </NotableDate>
  </NotableDates>
</NotableDateResource>
```

## 17. Example 2 — ANZAC Day observed in Australia and New Zealand

This example demonstrates:

- a single notable-date concept shared across multiple countries;
- territory-specific rules under the same concept;
- no reliance on display-name matching;
- the ability to attach jurisdiction-specific adjustment policies later without changing the concept identity.

The example deliberately keeps the baseline rule as the actual fixed date, 25 April, for both Australia and New Zealand. If a jurisdiction requires Mondayisation, substitution, or a regional public-holiday rule, that can be layered through a specific adjustment policy or override.

```xml
<NotableDateResource schemaVersion="1.0" resourceId="boduglobal.anzac">
  <ResolutionPolicy
    duplicatePolicy="Error"
    sameDayCollisionPolicy="KeepAll"
    spanCollisionPolicy="KeepAll"
    priorityDirection="HigherWins"
    observedDateRangePolicy="ObservedOccurrenceControlsInclusion" />

  <NotableDates>
    <NotableDate
      id="anzac-day"
      displayName="ANZAC Day"
      category="Remembrance"
      defaultDurationDays="1"
      defaultNonWorkingDay="true">
      <Tags>
        <Tag value="anzac" />
        <Tag value="remembrance" />
        <Tag value="military" />
      </Tags>

      <Rules>
        <Rule id="au-fixed-apr-25" priority="100">
          <Applicability calendar="Gregorian" fromYear="1916">
            <Territory code="AU" />
          </Applicability>
          <Strategy>
            <Fixed month="April" day="25" />
          </Strategy>
        </Rule>

        <Rule id="nz-fixed-apr-25" priority="100">
          <Applicability calendar="Gregorian" fromYear="1916">
            <Territory code="NZ" />
          </Applicability>
          <Strategy>
            <Fixed month="April" day="25" />
          </Strategy>
        </Rule>
      </Rules>
    </NotableDate>
  </NotableDates>
</NotableDateResource>
```

If New Zealand Mondayisation is required, it can be added as a reusable policy and referenced only by the New Zealand rule:

```xml
<AdjustmentPolicy id="nz-weekend-public-holiday-mondayised" priority="100">
  <Scope>
    <Territory code="NZ" />
    <Category value="Remembrance" />
  </Scope>
  <Trigger type="FallsOn">
    <Weekday value="Saturday" />
    <Weekday value="Sunday" />
  </Trigger>
  <Action type="MoveToNextWorkingDay" maxSearchDays="7" />
  <Emission mode="ObservedOnly" reason="Mondayised public holiday" />
</AdjustmentPolicy>
```

Then attach it only where intended:

```xml
<Adjustments>
  <Adjustment policyRef="nz-weekend-public-holiday-mondayised" />
</Adjustments>
```

## 18. Example 3 — Constitution Day in the United States and Puerto Rico

This example demonstrates why a notable-date concept needs multiple rule variants.

The display name `Constitution Day` can exist in more than one jurisdiction, but the date may differ by territory:

- United States: 17 September.
- Puerto Rico: 25 July.

The schema should not solve this through duplicate display names or name-based override rules. It should model the concept once and define explicit territory-specific rules.

```xml
<NotableDateResource schemaVersion="1.0" resourceId="boduglobal.us">
  <ResolutionPolicy
    duplicatePolicy="Error"
    sameDayCollisionPolicy="KeepAll"
    spanCollisionPolicy="KeepAll"
    priorityDirection="HigherWins"
    observedDateRangePolicy="ObservedOccurrenceControlsInclusion" />

  <NotableDates>
    <NotableDate
      id="constitution-day"
      displayName="Constitution Day"
      category="Civic"
      defaultDurationDays="1"
      defaultNonWorkingDay="false">
      <Tags>
        <Tag value="constitution" />
        <Tag value="civic" />
      </Tags>

      <Rules>
        <Rule id="us-federal-sep-17" priority="100">
          <Applicability calendar="Gregorian" fromYear="1787">
            <Territory code="US" />
          </Applicability>
          <Strategy>
            <Fixed month="September" day="17" />
          </Strategy>
        </Rule>

        <Rule id="pr-jul-25" priority="200" nonWorking="true">
          <Applicability calendar="Gregorian">
            <Territory code="US-PR" />
          </Applicability>
          <Strategy>
            <Fixed month="July" day="25" />
          </Strategy>
          <Tags>
            <Tag value="puerto-rico" />
          </Tags>
        </Rule>
      </Rules>
    </NotableDate>
  </NotableDates>
</NotableDateResource>
```

The higher priority on the Puerto Rico rule does not mean it overwrites the United States federal rule globally. It only matters if a resolution context includes both `US` and `US-PR` and the collision policy asks the system to pick one. Normal territory filtering should return the rule applicable to the requested territory.

If the two Constitution Days are considered materially different concepts, the schema also permits modelling them as separate notable-date concepts:

```xml
<NotableDate id="us-constitution-day" displayName="Constitution Day" category="Civic" />
<NotableDate id="puerto-rico-constitution-day" displayName="Constitution Day" category="Civic" />
```

The key point is that the author decides explicitly. The runtime should not infer identity from display name alone.

## 19. Suggested XSD structure

The following XSD is intentionally presented as a strategic structure rather than a complete drop-in schema. It shows how the current XSD could evolve while retaining the existing vocabulary.

```xml
<?xml version="1.0" encoding="utf-8"?>
<xs:schema
  xmlns:xs="http://www.w3.org/2001/XMLSchema"
  xmlns:vc="http://www.w3.org/2007/XMLSchema-versioning"
  vc:minVersion="1.1"
  elementFormDefault="qualified"
  attributeFormDefault="unqualified"
  targetNamespace="urn:bodu:globalization:calendar"
  xmlns:nd="urn:bodu:globalization:calendar">

  <xs:element name="NotableDateResource" type="nd:NotableDateResource" />

  <xs:complexType name="NotableDateResource">
    <xs:sequence>
      <xs:element name="Metadata" type="nd:Metadata" minOccurs="0" maxOccurs="1" />
      <xs:element name="Imports" type="nd:Imports" minOccurs="0" maxOccurs="1" />
      <xs:element name="ResolutionPolicy" type="nd:ResolutionPolicy" minOccurs="0" maxOccurs="1" />
      <xs:element name="AdjustmentPolicies" type="nd:AdjustmentPolicies" minOccurs="0" maxOccurs="1" />
      <xs:element name="NotableDates" type="nd:NotableDates" minOccurs="0" maxOccurs="1" />
      <xs:element name="Overrides" type="nd:Overrides" minOccurs="0" maxOccurs="1" />
    </xs:sequence>
    <xs:attribute name="schemaVersion" type="xs:string" use="required" />
    <xs:attribute name="resourceId" type="nd:identifier" use="required" />
  </xs:complexType>

  <xs:complexType name="Metadata">
    <xs:sequence>
      <xs:element name="Name" type="xs:string" minOccurs="0" maxOccurs="1" />
      <xs:element name="Description" type="xs:string" minOccurs="0" maxOccurs="1" />
      <xs:element name="Source" type="xs:string" minOccurs="0" maxOccurs="unbounded" />
    </xs:sequence>
  </xs:complexType>

  <xs:complexType name="Imports">
    <xs:sequence>
      <xs:element name="Import" type="nd:Import" minOccurs="1" maxOccurs="unbounded" />
    </xs:sequence>
  </xs:complexType>

  <xs:complexType name="Import">
    <xs:choice minOccurs="1" maxOccurs="unbounded">
      <xs:element name="IncludeAll" type="nd:EmptyElement" />
      <xs:element name="Include" type="nd:Include" />
    </xs:choice>
    <xs:attribute name="resource" type="xs:string" use="required" />
  </xs:complexType>

  <xs:complexType name="Include">
    <xs:sequence>
      <xs:element name="PatchRule" type="nd:PatchRule" minOccurs="0" maxOccurs="unbounded" />
    </xs:sequence>
    <xs:attribute name="notableDateRef" type="nd:identifier" use="required" />
  </xs:complexType>

  <xs:complexType name="ResolutionPolicy">
    <xs:attribute name="duplicatePolicy" type="nd:duplicatePolicy" use="optional" default="Error" />
    <xs:attribute name="sameDayCollisionPolicy" type="nd:collisionPolicy" use="optional" default="KeepAll" />
    <xs:attribute name="spanCollisionPolicy" type="nd:collisionPolicy" use="optional" default="KeepAll" />
    <xs:attribute name="priorityDirection" type="nd:priorityDirection" use="optional" default="HigherWins" />
    <xs:attribute name="observedDateRangePolicy" type="nd:observedDateRangePolicy" use="optional" default="ObservedOccurrenceControlsInclusion" />
  </xs:complexType>

  <xs:complexType name="AdjustmentPolicies">
    <xs:sequence>
      <xs:element name="AdjustmentPolicy" type="nd:AdjustmentPolicy" minOccurs="1" maxOccurs="unbounded" />
    </xs:sequence>
  </xs:complexType>

  <xs:complexType name="AdjustmentPolicy">
    <xs:sequence>
      <xs:element name="Scope" type="nd:AdjustmentScope" minOccurs="0" maxOccurs="1" />
      <xs:element name="Trigger" type="nd:AdjustmentTrigger" minOccurs="1" maxOccurs="1" />
      <xs:element name="Action" type="nd:AdjustmentAction" minOccurs="1" maxOccurs="1" />
      <xs:element name="Emission" type="nd:AdjustmentEmission" minOccurs="1" maxOccurs="1" />
    </xs:sequence>
    <xs:attribute name="id" type="nd:identifier" use="required" />
    <xs:attribute name="priority" type="xs:int" use="optional" default="0" />
    <xs:attribute name="description" type="xs:string" use="optional" />
  </xs:complexType>

  <xs:complexType name="AdjustmentScope">
    <xs:choice minOccurs="0" maxOccurs="unbounded">
      <xs:element name="Territory" type="nd:TerritoryRef" />
      <xs:element name="Calendar" type="nd:CalendarRef" />
      <xs:element name="Category" type="nd:CategoryRef" />
      <xs:element name="NotableDate" type="nd:NotableDateRef" />
      <xs:element name="Rule" type="nd:RuleRef" />
    </xs:choice>
  </xs:complexType>

  <xs:complexType name="NotableDates">
    <xs:sequence>
      <xs:element name="NotableDate" type="nd:NotableDate" minOccurs="1" maxOccurs="unbounded" />
    </xs:sequence>
  </xs:complexType>

  <xs:complexType name="NotableDate">
    <xs:sequence>
      <xs:element name="Tags" type="nd:Tags" minOccurs="0" maxOccurs="1" />
      <xs:element name="Rules" type="nd:Rules" minOccurs="1" maxOccurs="1" />
    </xs:sequence>
    <xs:attribute name="id" type="nd:identifier" use="required" />
    <xs:attribute name="displayName" type="xs:string" use="required" />
    <xs:attribute name="category" type="nd:notableDateCategory" use="required" />
    <xs:attribute name="defaultDurationDays" type="xs:positiveInteger" use="optional" default="1" />
    <xs:attribute name="defaultNonWorkingDay" type="xs:boolean" use="optional" />
  </xs:complexType>

  <xs:complexType name="Rules">
    <xs:sequence>
      <xs:element name="Rule" type="nd:Rule" minOccurs="1" maxOccurs="unbounded" />
    </xs:sequence>
  </xs:complexType>

  <xs:complexType name="Rule">
    <xs:sequence>
      <xs:element name="Applicability" type="nd:Applicability" minOccurs="0" maxOccurs="1" />
      <xs:element name="Strategy" type="nd:Strategy" minOccurs="1" maxOccurs="1" />
      <xs:element name="Tags" type="nd:Tags" minOccurs="0" maxOccurs="1" />
      <xs:element name="Adjustments" type="nd:RuleAdjustments" minOccurs="0" maxOccurs="1" />
    </xs:sequence>
    <xs:attribute name="id" type="nd:identifier" use="required" />
    <xs:attribute name="priority" type="xs:int" use="optional" default="0" />
    <xs:attribute name="category" type="nd:notableDateCategory" use="optional" />
    <xs:attribute name="nonWorking" type="xs:boolean" use="optional" />
    <xs:attribute name="durationDays" type="xs:positiveInteger" use="optional" />
    <xs:attribute name="comment" type="xs:string" use="optional" />
  </xs:complexType>

  <xs:complexType name="Applicability">
    <xs:sequence>
      <xs:element name="Territory" type="nd:TerritoryRef" minOccurs="0" maxOccurs="unbounded" />
      <xs:element name="OnlyYear" type="nd:YearRef" minOccurs="0" maxOccurs="unbounded" />
      <xs:element name="ExceptYear" type="nd:YearRef" minOccurs="0" maxOccurs="unbounded" />
    </xs:sequence>
    <xs:attribute name="calendar" type="nd:calendarName" use="optional" default="Gregorian" />
    <xs:attribute name="fromYear" type="xs:unsignedShort" use="optional" />
    <xs:attribute name="toYear" type="xs:unsignedShort" use="optional" />
  </xs:complexType>

  <xs:complexType name="Strategy">
    <xs:choice minOccurs="1" maxOccurs="1">
      <xs:element name="Fixed" type="nd:FixedStrategy" />
      <xs:element name="DayOfWeekInMonth" type="nd:DayOfWeekInMonthStrategy" />
      <xs:element name="WeekdayNearDate" type="nd:WeekdayNearDateStrategy" />
      <xs:element name="RelativeWeekdayInMonth" type="nd:RelativeWeekdayInMonthStrategy" />
      <xs:element name="OffsetFromRule" type="nd:OffsetFromRuleStrategy" />
      <xs:element name="Algorithm" type="nd:AlgorithmStrategy" />
    </xs:choice>
  </xs:complexType>

  <xs:complexType name="FixedStrategy">
    <xs:attribute name="month" type="nd:monthOrNumber" use="required" />
    <xs:attribute name="day" type="nd:day" use="required" />
    <xs:attribute name="skipLeapMonth" type="xs:boolean" use="optional" />
    <xs:attribute name="sweepCalendarYears" type="xs:boolean" use="optional" />
  </xs:complexType>

  <xs:complexType name="DayOfWeekInMonthStrategy">
    <xs:attribute name="month" type="nd:monthName" use="required" />
    <xs:attribute name="dayOfWeek" type="nd:dayOfWeek" use="required" />
    <xs:attribute name="weekOrdinal" type="nd:weekOrdinal" use="required" />
  </xs:complexType>

  <xs:complexType name="WeekdayNearDateStrategy">
    <xs:attribute name="month" type="nd:monthName" use="required" />
    <xs:attribute name="day" type="nd:day" use="required" />
    <xs:attribute name="dayOfWeek" type="nd:dayOfWeek" use="required" />
    <xs:attribute name="direction" type="nd:weekdayProximity" use="required" />
  </xs:complexType>

  <xs:complexType name="RelativeWeekdayInMonthStrategy">
    <xs:attribute name="month" type="nd:monthName" use="required" />
    <xs:attribute name="dayOfWeek" type="nd:dayOfWeek" use="required" />
    <xs:attribute name="weekOrdinal" type="nd:weekOrdinal" use="required" />
    <xs:attribute name="relativeDayOfWeek" type="nd:dayOfWeek" use="required" />
    <xs:attribute name="direction" type="nd:weekdayProximity" use="required" />
  </xs:complexType>

  <xs:complexType name="OffsetFromRuleStrategy">
    <xs:attribute name="notableDateRef" type="nd:identifier" use="required" />
    <xs:attribute name="ruleRef" type="nd:identifier" use="optional" />
    <xs:attribute name="offsetDays" type="xs:int" use="required" />
  </xs:complexType>

  <xs:complexType name="AlgorithmStrategy">
    <xs:sequence>
      <xs:element name="Parameter" type="nd:Parameter" minOccurs="0" maxOccurs="unbounded" />
    </xs:sequence>
    <xs:attribute name="key" type="xs:string" use="required" />
  </xs:complexType>

  <xs:complexType name="RuleAdjustments">
    <xs:choice minOccurs="1" maxOccurs="unbounded">
      <xs:element name="Adjustment" type="nd:AdjustmentRef" />
      <xs:element name="InlineAdjustment" type="nd:AdjustmentPolicy" />
    </xs:choice>
  </xs:complexType>

  <xs:complexType name="AdjustmentRef">
    <xs:attribute name="policyRef" type="nd:identifier" use="required" />
  </xs:complexType>

  <xs:complexType name="AdjustmentTrigger">
    <xs:sequence>
      <xs:element name="Weekday" type="nd:WeekdayRef" minOccurs="0" maxOccurs="unbounded" />
      <xs:element name="Target" type="nd:CollisionTarget" minOccurs="0" maxOccurs="1" />
      <xs:element name="Parameter" type="nd:Parameter" minOccurs="0" maxOccurs="unbounded" />
    </xs:sequence>
    <xs:attribute name="type" type="nd:adjustmentTrigger" use="required" />
  </xs:complexType>

  <xs:complexType name="AdjustmentAction">
    <xs:sequence>
      <xs:element name="Parameter" type="nd:Parameter" minOccurs="0" maxOccurs="unbounded" />
    </xs:sequence>
    <xs:attribute name="type" type="nd:adjustmentAction" use="required" />
    <xs:attribute name="days" type="xs:int" use="optional" />
    <xs:attribute name="dayOfWeek" type="nd:dayOfWeek" use="optional" />
    <xs:attribute name="maxSearchDays" type="xs:positiveInteger" use="optional" />
    <xs:attribute name="skipWeekends" type="xs:boolean" use="optional" />
    <xs:attribute name="skipNonWorkingDates" type="xs:boolean" use="optional" />
    <xs:attribute name="notableDateRef" type="nd:identifier" use="optional" />
    <xs:attribute name="ruleRef" type="nd:identifier" use="optional" />
    <xs:attribute name="handlerKey" type="xs:string" use="optional" />
  </xs:complexType>

  <xs:complexType name="AdjustmentEmission">
    <xs:attribute name="mode" type="nd:emissionMode" use="required" />
    <xs:attribute name="reason" type="xs:string" use="optional" />
    <xs:attribute name="nonWorking" type="xs:boolean" use="optional" />
  </xs:complexType>

  <xs:complexType name="Overrides">
    <xs:choice minOccurs="1" maxOccurs="unbounded">
      <xs:element name="AddNotableDate" type="nd:NotableDate" />
      <xs:element name="PatchNotableDate" type="nd:PatchNotableDate" />
      <xs:element name="RemoveNotableDate" type="nd:NotableDateRef" />
      <xs:element name="AddRule" type="nd:AddRule" />
      <xs:element name="PatchRule" type="nd:PatchRule" />
      <xs:element name="ReplaceRule" type="nd:ReplaceRule" />
      <xs:element name="RemoveRule" type="nd:RuleRef" />
      <xs:element name="AddAdjustmentPolicy" type="nd:AdjustmentPolicy" />
      <xs:element name="PatchAdjustmentPolicy" type="nd:PatchAdjustmentPolicy" />
      <xs:element name="RemoveAdjustmentPolicy" type="nd:AdjustmentPolicyRef" />
    </xs:choice>
  </xs:complexType>

  <xs:complexType name="PatchRule">
    <xs:sequence>
      <xs:element name="Applicability" type="nd:Applicability" minOccurs="0" maxOccurs="1" />
      <xs:element name="Strategy" type="nd:Strategy" minOccurs="0" maxOccurs="1" />
      <xs:element name="Tags" type="nd:Tags" minOccurs="0" maxOccurs="1" />
      <xs:element name="Adjustments" type="nd:RuleAdjustments" minOccurs="0" maxOccurs="1" />
    </xs:sequence>
    <xs:attribute name="notableDateRef" type="nd:identifier" use="required" />
    <xs:attribute name="ruleRef" type="nd:identifier" use="required" />
    <xs:attribute name="priority" type="xs:int" use="optional" />
    <xs:attribute name="category" type="nd:notableDateCategory" use="optional" />
    <xs:attribute name="nonWorking" type="xs:boolean" use="optional" />
    <xs:attribute name="durationDays" type="xs:positiveInteger" use="optional" />
    <xs:attribute name="comment" type="xs:string" use="optional" />
  </xs:complexType>

  <!-- Additional patch/add/replace types can reuse PatchRule, Rule, and NotableDate shapes. -->

</xs:schema>
```

## 20. Supporting simple types

The new XSD should reuse most of the current simple types, with some refinements.

```xml
<xs:simpleType name="identifier">
  <xs:restriction base="xs:string">
    <xs:pattern value="^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$" />
  </xs:restriction>
</xs:simpleType>
```

Recommended policy enums:

```xml
<xs:simpleType name="duplicatePolicy">
  <xs:restriction base="xs:string">
    <xs:enumeration value="Error" />
    <xs:enumeration value="KeepFirst" />
    <xs:enumeration value="KeepLast" />
    <xs:enumeration value="Merge" />
  </xs:restriction>
</xs:simpleType>

<xs:simpleType name="collisionPolicy">
  <xs:restriction base="xs:string">
    <xs:enumeration value="KeepAll" />
    <xs:enumeration value="HighestPriorityOnly" />
    <xs:enumeration value="CategoryPriority" />
    <xs:enumeration value="Custom" />
  </xs:restriction>
</xs:simpleType>

<xs:simpleType name="priorityDirection">
  <xs:restriction base="xs:string">
    <xs:enumeration value="HigherWins" />
    <xs:enumeration value="LowerWins" />
  </xs:restriction>
</xs:simpleType>

<xs:simpleType name="observedDateRangePolicy">
  <xs:restriction base="xs:string">
    <xs:enumeration value="ObservedOccurrenceControlsInclusion" />
    <xs:enumeration value="ActualOccurrenceControlsInclusion" />
    <xs:enumeration value="BothOccurrencesControlInclusion" />
  </xs:restriction>
</xs:simpleType>

<xs:simpleType name="emissionMode">
  <xs:restriction base="xs:string">
    <xs:enumeration value="ActualOnly" />
    <xs:enumeration value="ObservedOnly" />
    <xs:enumeration value="ActualAndObserved" />
    <xs:enumeration value="ObservedAsAdditional" />
    <xs:enumeration value="Suppress" />
  </xs:restriction>
</xs:simpleType>
```

Territory should no longer be comma-delimited:

```xml
<xs:simpleType name="territoryCode">
  <xs:restriction base="xs:string">
    <xs:pattern value="^[A-Z]{2}(-[A-Z0-9]{2,3})?$" />
  </xs:restriction>
</xs:simpleType>
```

## 21. Validation requirements

The schema and runtime validator should enforce the following rules.

| Validation | Required behaviour |
|---|---|
| Duplicate `resourceId` in the load graph | Error unless explicit replacement is configured. |
| Duplicate `notableDate.id` within a resolved resource graph | Error. |
| Duplicate `rule.id` within a notable date | Error. |
| Duplicate full rule identity after imports | Error by default. |
| `displayName` duplicates | Allowed, but never used as identity. |
| `OffsetFromRule` target resolves to zero rules | Error. |
| `OffsetFromRule` target resolves to multiple rules without `ruleRef` | Error. |
| `ReplaceWithRule` target resolves to zero or multiple rules | Error. |
| Adjustment policy reference cannot be resolved | Error. |
| Calendar/month mismatch | Error. |
| Missing algorithm handler | Error or warning based on strictness mode. |
| Custom adjustment without `maxSearchDays` or declared reach | Warning or error. |
| Territory code malformed | Error. |
| `fromYear` greater than `toYear` | Error. |
| `OnlyYear` outside year range | Warning or error. |
| `ExceptYear` outside year range | Warning. |

## 22. Runtime resolution strategy

The runtime should resolve notable dates through a deterministic pipeline:

```text
1. Load resource graph.
2. Resolve imports by stable resource and notable-date IDs.
3. Apply import patches.
4. Apply explicit override operations.
5. Validate identities, references, calendars, territories, and adjustment reach.
6. Select rules applicable to the requested territory/calendar/year range.
7. Resolve base occurrences from each rule strategy.
8. Apply adjustment policies by priority and scope.
9. Apply adjustment emission mode.
10. Apply duplicate and collision policy.
11. Return results with source metadata.
```

Returned notable-date results should carry source metadata:

```text
resourceId
notableDateId
ruleId
adjustmentPolicyId, if any
actualDate, if different from emitted date
isObserved
adjustmentReason
```

This allows diagnostics, auditability, and supportability.

## 23. Implementation migration from the current schema

The current schema can be migrated in stages.

### Stage 1 — introduce IDs while preserving current names

- Add `id` to `NotableDate`.
- Rename or supplement current `Rule name` with `id`.
- Keep `name` or `displayName` as presentation text.
- Internally build rule identity from `(resourceId, notableDate.id, rule.id)`.

### Stage 2 — normalise territories

- Replace comma-delimited `territory` attributes with repeated `<Territory code="..." />` elements.
- Keep parser compatibility for legacy comma-delimited values behind a legacy mode.

### Stage 3 — split adjustment policy from inline adjustment

- Add resource-level `<AdjustmentPolicies>`.
- Keep inline `<Adjustment>` support for simple cases.
- Prefer `<Adjustment policyRef="..." />` in rules.

### Stage 4 — convert UseFrom to ID-based imports

- Preserve `UseFrom` as a compatibility alias.
- Introduce `<Imports>` / `<Import>` / `<Include>` using `notableDateRef`.
- Replace nested override `name` semantics with explicit `ruleRef`.

### Stage 5 — introduce explicit override operations

- Add `<Overrides>` with `PatchRule`, `ReplaceRule`, `RemoveRule`, etc.
- Deprecate broad name-based removals.

### Stage 6 — enforce validation

- Add strict validation mode.
- Run strict validation in tests and CI.
- Allow permissive legacy parsing only where explicitly requested.

## 24. Key recommendation

The preferred schema should not treat a notable date as a flat list of named rules. It should treat the cookbook as a small domain model:

```text
Resource
  contains NotableDate concepts
    each concept contains Rule variants
      each rule has Applicability + Strategy + Adjustment references
  contains reusable AdjustmentPolicy definitions
  contains explicit Import and Override operations
```

That model directly solves the central design issue: it allows a notable date such as `Constitution Day` to have multiple territory-specific rules, and a shared date such as `ANZAC Day` to have country-specific rules and adjustment policies, without obscuring one rule behind another or relying on fragile display-name matching.
