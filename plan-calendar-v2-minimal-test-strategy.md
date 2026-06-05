# Minimal Test Strategy for the New Calendar Cookbook Schema

## Purpose

This document defines a simplified first-version test strategy for a new Calendar library modelled from the existing implementation, but rebuilt around the revised cookbook schema.

The aim is not to test every possible calendar rule in the first implementation. The aim is to validate that the minimum functional path works and that the new design fixes the key problems identified in the existing implementation:

- notable-date rules must not collapse by display name or canonical name;
- territory-specific variants must remain distinct;
- observed-date adjustments must behave consistently for single-day and range queries;
- overrides must target exact rule identities;
- fixed-date resolution must work from a small schema-driven cookbook.

The first implementation should focus on a small deterministic fixture and a focused set of tests with clear expected results.

---

## First-Version Scope

The first base version should support only the following capabilities:

```text
NotableDate
Rule
Applicability
FixedDate strategy
AdjustmentPolicy
ObservedOnly emission
Territory filtering
Rule identity
Basic override add/remove/patch
```

The following capabilities should be deferred until after the base model is proven:

```text
Algorithm rules
Offset rules
Nth weekday rules
Relative weekday rules
Multi-day events
Complex collision policies
Non-Gregorian calendars
Custom adjustments
Large import graphs
Advanced prioritisation
```

---

## Minimal Notable-Date Fixture

Use only three notable date concepts in the first test fixture.

| Notable date | Rules | Purpose |
|---|---:|---|
| New Year’s Day | AU fixed 1-Jan | Tests fixed date plus weekend observed adjustment. |
| ANZAC Day | AU fixed 25-Apr, NZ fixed 25-Apr | Tests same notable-date concept across different territories. |
| Constitution Day | US fixed 17-Sep, PR fixed 25-Jul | Tests same notable-date concept with territory-specific different dates. |

This fixture is intentionally small, but it validates the most important design behaviours.

---

## Minimal Cookbook Structure

The test resource should conceptually contain the following entries:

```text
new-years-day
 └─ au-fixed-jan-1
    ├─ territory: AU
    ├─ fixed date: 1-Jan
    └─ adjustment: weekend-to-next-monday

anzac-day
 ├─ au-fixed-apr-25
 │  ├─ territory: AU
 │  └─ fixed date: 25-Apr
 └─ nz-fixed-apr-25
    ├─ territory: NZ
    └─ fixed date: 25-Apr

constitution-day
 ├─ us-fixed-sep-17
 │  ├─ territory: US
 │  └─ fixed date: 17-Sep
 └─ pr-fixed-jul-25
    ├─ territory: PR
    └─ fixed date: 25-Jul
```

The important design point is that `anzac-day` and `constitution-day` each have multiple rules under the same notable-date concept. This proves that the new schema does not collapse rules by display name or notable-date name.

---

## Minimal XML Fixture

The first test fixture can use the following XML structure.

```xml
<NotableDateResource schemaVersion="1.0" resourceId="test.minimal">

  <AdjustmentPolicies>
    <AdjustmentPolicy id="weekend-to-next-monday" priority="100">
      <Trigger type="fallsOn">
        <Weekday value="Saturday" />
        <Weekday value="Sunday" />
      </Trigger>
      <Action type="moveToNextWeekday" weekday="Monday" />
      <Emission mode="observedOnly" reason="Observed public holiday" />
    </AdjustmentPolicy>
  </AdjustmentPolicies>

  <NotableDates>

    <NotableDate
      id="new-years-day"
      displayName="New Year's Day"
      category="PublicHoliday"
      defaultNonWorkingDay="true">

      <Rules>
        <Rule id="au-fixed-jan-1" priority="100">
          <Applicability calendar="Gregorian">
            <Territory code="AU" />
          </Applicability>
          <Strategy type="fixedDate" month="1" day="1" />
          <Adjustments>
            <Adjustment policyRef="weekend-to-next-monday" />
          </Adjustments>
        </Rule>
      </Rules>
    </NotableDate>

    <NotableDate
      id="anzac-day"
      displayName="ANZAC Day"
      category="PublicHoliday"
      defaultNonWorkingDay="true">

      <Rules>
        <Rule id="au-fixed-apr-25" priority="100">
          <Applicability calendar="Gregorian">
            <Territory code="AU" />
          </Applicability>
          <Strategy type="fixedDate" month="4" day="25" />
        </Rule>

        <Rule id="nz-fixed-apr-25" priority="100">
          <Applicability calendar="Gregorian">
            <Territory code="NZ" />
          </Applicability>
          <Strategy type="fixedDate" month="4" day="25" />
        </Rule>
      </Rules>
    </NotableDate>

    <NotableDate
      id="constitution-day"
      displayName="Constitution Day"
      category="Observance"
      defaultNonWorkingDay="false">

      <Rules>
        <Rule id="us-fixed-sep-17" priority="100">
          <Applicability calendar="Gregorian">
            <Territory code="US" />
          </Applicability>
          <Strategy type="fixedDate" month="9" day="17" />
        </Rule>

        <Rule id="pr-fixed-jul-25" priority="100">
          <Applicability calendar="Gregorian">
            <Territory code="PR" />
          </Applicability>
          <Strategy type="fixedDate" month="7" day="25" />
        </Rule>
      </Rules>
    </NotableDate>

  </NotableDates>
</NotableDateResource>
```

---

## Minimal Expected Result Model

The initial test assertions should use a simple expected result model.

```csharp
public sealed record ExpectedNotableDate(
    DateOnly Date,
    DateOnly? ActualDate,
    bool IsObserved,
    string NotableDateId,
    string RuleId,
    string DisplayName,
    string TerritoryCode,
    string Category,
    string? AdjustmentPolicyId = null);
```

This is intentionally small, but still proves that the resolver selected the correct notable date, rule, territory, category, and adjustment behaviour.

---

# Minimal Test Catalogue

## T01 — Load Minimal Cookbook

### Purpose

Validate the schema and load pipeline.

### Given

The minimal cookbook resource.

### When

The resource is loaded and validated.

### Then

| Item | Expected |
|---|---:|
| Notable dates | 3 |
| Rules | 5 |
| Adjustment policies | 1 |

### Expected Outcome

```text
Validation succeeds.
3 notable-date concepts are loaded.
5 rules are loaded.
1 adjustment policy is loaded.
```

---

## T02 — Resolve New Year’s Day on a Weekday

### Purpose

Validate baseline fixed-date resolution.

### Query

```text
Date: 2026-01-01
Territory: AU
```

1 January 2026 is a Thursday, so no weekend adjustment applies.

### Expected Result

| Field | Expected |
|---|---|
| Date | 2026-01-01 |
| ActualDate | 2026-01-01 |
| IsObserved | false |
| NotableDateId | new-years-day |
| RuleId | au-fixed-jan-1 |
| DisplayName | New Year’s Day |
| Territory | AU |
| Category | PublicHoliday |
| AdjustmentPolicyId | null |

---

## T03 — Resolve New Year’s Day Weekend Adjustment

### Purpose

Validate the basic observed-date adjustment path.

### Query

```text
Date: 2022-01-03
Territory: AU
```

1 January 2022 was a Saturday. The fixture observes weekend holidays on the following Monday.

### Expected Result

| Field | Expected |
|---|---|
| Date | 2022-01-03 |
| ActualDate | 2022-01-01 |
| IsObserved | true |
| NotableDateId | new-years-day |
| RuleId | au-fixed-jan-1 |
| DisplayName | New Year’s Day |
| Territory | AU |
| Category | PublicHoliday |
| AdjustmentPolicyId | weekend-to-next-monday |

---

## T04 — Observed-Only Adjustment Suppresses Base Date

### Purpose

Validate that `observedOnly` means the actual/base date is not emitted when an observed date applies.

### Query

```text
Date: 2022-01-01
Territory: AU
```

### Expected Result

```text
No New Year’s Day result is returned.
```

### Reason

The adjustment policy uses:

```text
Emission mode = observedOnly
```

Therefore, when the fixed date falls on a weekend and is observed on Monday, the base Saturday occurrence is suppressed.

---

## T05 — Single-Day and Range Queries Are Consistent

### Purpose

Validate that observed-date behaviour does not depend on query width.

This is one of the most important regression tests against the existing implementation.

### Queries and Expected Results

| Query | Expected |
|---|---|
| 2022-01-01 only | No New Year’s Day result |
| 2022-01-03 only | One New Year’s Day observed result on 2022-01-03 |
| 2022-01-01 to 2022-01-03 | One New Year’s Day observed result on 2022-01-03 |

### Expected Outcome

The resolver should not return the base date for one query and the observed date for another query merely because the query range changed.

---

## T06 — Resolve ANZAC Day for Australia

### Purpose

Validate a territory-specific fixed-date rule.

### Query

```text
Date: 2026-04-25
Territory: AU
```

### Expected Result

| Field | Expected |
|---|---|
| Date | 2026-04-25 |
| ActualDate | 2026-04-25 |
| IsObserved | false |
| NotableDateId | anzac-day |
| RuleId | au-fixed-apr-25 |
| DisplayName | ANZAC Day |
| Territory | AU |
| Category | PublicHoliday |

---

## T07 — Resolve ANZAC Day for New Zealand

### Purpose

Validate that the same notable-date concept can have a separate New Zealand rule.

### Query

```text
Date: 2026-04-25
Territory: NZ
```

### Expected Result

| Field | Expected |
|---|---|
| Date | 2026-04-25 |
| ActualDate | 2026-04-25 |
| IsObserved | false |
| NotableDateId | anzac-day |
| RuleId | nz-fixed-apr-25 |
| DisplayName | ANZAC Day |
| Territory | NZ |
| Category | PublicHoliday |

---

## T08 — ANZAC Day Does Not Leak Into Another Territory

### Purpose

Validate basic territory scoping.

### Query

```text
Date: 2026-04-25
Territory: US
```

### Expected Result

```text
No ANZAC Day result is returned.
```

---

## T09 — Resolve US Constitution Day

### Purpose

Validate territory-specific fixed date where the notable-date concept has multiple rules with different dates.

### Query

```text
Date: 2026-09-17
Territory: US
```

### Expected Result

| Field | Expected |
|---|---|
| Date | 2026-09-17 |
| ActualDate | 2026-09-17 |
| IsObserved | false |
| NotableDateId | constitution-day |
| RuleId | us-fixed-sep-17 |
| DisplayName | Constitution Day |
| Territory | US |
| Category | Observance |

---

## T10 — Resolve Puerto Rico Constitution Day

### Purpose

Validate that the same notable-date concept can have a different fixed date for a different territory.

### Query

```text
Date: 2026-07-25
Territory: PR
```

### Expected Result

| Field | Expected |
|---|---|
| Date | 2026-07-25 |
| ActualDate | 2026-07-25 |
| IsObserved | false |
| NotableDateId | constitution-day |
| RuleId | pr-fixed-jul-25 |
| DisplayName | Constitution Day |
| Territory | PR |
| Category | Observance |

---

## T11 — Puerto Rico Constitution Day Does Not Return US Date

### Purpose

Validate that one rule does not obscure, replace, or leak into another territory-specific rule.

### Query

```text
Range: 2026-07-01 to 2026-09-30
Territory: PR
```

### Expected Results

| Date | Expected |
|---|---|
| 2026-07-25 | Constitution Day / pr-fixed-jul-25 |
| 2026-09-17 | No Constitution Day result for PR |

### Design Guarantee

This directly proves that the resolver is not keyed only by display name or notable-date name.

---

## T12 — Remove One Rule Without Removing Sibling Rules

### Purpose

Validate exact override targeting.

### Override

```text
Remove rule:
notableDateId = constitution-day
ruleId = pr-fixed-jul-25
```

### Queries and Expected Results

| Query | Expected |
|---|---|
| US / 2026-09-17 | Constitution Day returned |
| PR / 2026-07-25 | Constitution Day not returned |

### Design Guarantee

Removing one rule must not remove all rules with the same display name or notable-date concept.

---

## T13 — Patch One Rule Without Patching Sibling Rules

### Purpose

Validate exact patch targeting.

### Override

```text
Patch rule:
notableDateId = constitution-day
ruleId = pr-fixed-jul-25
category = PublicHoliday
```

### Expected Results

| Rule | Expected Category |
|---|---|
| constitution-day / us-fixed-sep-17 | Observance |
| constitution-day / pr-fixed-jul-25 | PublicHoliday |

### Design Guarantee

Patching one rule must not patch sibling rules under the same notable-date concept.

---

# Suggested Test Names

The first implementation should use scenario-focused test names.

```csharp
[TestMethod]
public void LoadMinimalCookbook_ReturnsExpectedCounts()

[TestMethod]
public void Resolve_NewYearsDay_WhenWeekday_ReturnsActualDate()

[TestMethod]
public void Resolve_NewYearsDay_WhenWeekend_ReturnsObservedDate()

[TestMethod]
public void Resolve_NewYearsDay_WhenObservedOnly_DoesNotReturnBaseDate()

[TestMethod]
public void Resolve_NewYearsDay_WhenSingleDayAndRangeQueriesUsed_ReturnsConsistentObservedResult()

[TestMethod]
public void Resolve_AnzacDay_WhenTerritoryIsAustralia_ReturnsAustralianRule()

[TestMethod]
public void Resolve_AnzacDay_WhenTerritoryIsNewZealand_ReturnsNewZealandRule()

[TestMethod]
public void Resolve_AnzacDay_WhenTerritoryIsUnitedStates_ReturnsNoResult()

[TestMethod]
public void Resolve_ConstitutionDay_WhenTerritoryIsUnitedStates_ReturnsSeptember17Rule()

[TestMethod]
public void Resolve_ConstitutionDay_WhenTerritoryIsPuertoRico_ReturnsJuly25Rule()

[TestMethod]
public void Resolve_ConstitutionDay_WhenTerritoryIsPuertoRico_DoesNotReturnUnitedStatesRule()

[TestMethod]
public void Override_RemoveRule_WhenTargetingPuertoRicoConstitutionDay_DoesNotRemoveUnitedStatesRule()

[TestMethod]
public void Override_PatchRule_WhenTargetingPuertoRicoConstitutionDay_DoesNotPatchUnitedStatesRule()
```

---

# What This Minimal Set Proves

This reduced test set proves the essential first-version behaviours:

```text
1. The new schema can load a small cookbook.
2. Fixed-date rules resolve correctly.
3. A notable date can contain multiple rules.
4. Rules are identified by stable IDs.
5. Territory-specific variants remain distinct.
6. Rules do not leak across territories.
7. Observed-date adjustments can move dates.
8. Observed-only adjustments suppress the base date.
9. Single-day and range queries behave consistently.
10. Overrides target exact rules, not display names.
```

This is enough to validate the minimum functional implementation while directly addressing the central design flaws in the existing library.

---

# Deferred Tests for Later Versions

The following tests should be added after the base fixed-date model is stable:

| Later Area | Example |
|---|---|
| Algorithm rules | Easter Sunday |
| Offset rules | Good Friday relative to Easter |
| Nth weekday rules | Second Monday in June |
| Relative weekday rules | Monday before last day of month |
| Weekday-near-date rules | Nearest Monday to a fixed date |
| Multi-day events | Festival spanning multiple days |
| Collision policy | Same-day priority handling |
| Complex imports | Import selected rules from a shared resource |
| Non-Gregorian calendars | Hebrew or Islamic calendar dates |
| Custom adjustments | Custom handler with max search range |
| Advanced validation | Ambiguous references, unresolved algorithms, duplicate IDs |

These should not block the first base version.

---

# Recommended First Implementation Exit Criteria

The first base version can be considered structurally sound when all of the following are true:

```text
The minimal cookbook loads successfully.
The 5 fixed-date rules resolve correctly.
Territory filtering is correct.
The same notable-date ID can safely contain multiple rule IDs.
Observed-only adjustment behaviour is deterministic.
Range and single-day queries return consistent observed-date results.
Remove and patch overrides target only the intended rule.
```

Once those conditions are met, the implementation has a solid foundation for adding the remaining cookbook strategies.
