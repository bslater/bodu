// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Parses a notable-date document XML document into its component model objects, validating it against the embedded XSD
/// and collecting structural and semantic diagnostics.
/// </summary>
internal static class NotableDateDocumentParser
{
    /// <summary>
    /// The manifest resource name of the embedded v2 notable-date document XSD.
    /// </summary>
    private const string SchemaResourceName = "Bodu.Globalization.Calendar.V2.NotableDates.v2.xsd";

    /// <summary>
    /// The XML namespace of the v2 notable-date document vocabulary.
    /// </summary>
    private static readonly XNamespace s_ns = "urn:bodu:globalization:calendar";

    /// <summary>
    /// The full English month names, indexed so that January is at index zero.
    /// </summary>
    private static readonly string[] s_monthNames =
    {
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December",
    };

    /// <summary>
    /// Parses and schema-validates a notable-date document XML document.
    /// </summary>
    /// <param name="xml">The notable-date document XML content.</param>
    /// <param name="diagnostics">The collection that receives structural and semantic diagnostics.</param>
    /// <returns>The parsed <see cref="ParsedNotableDateDocument" />.</returns>
    /// <exception cref="FormatException"><paramref name="xml" /> is not well-formed XML.</exception>
    public static ParsedNotableDateDocument Parse(string xml, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        XDocument document;
        try
        {
            document = XDocument.Parse(xml, LoadOptions.None);
        }
        catch (XmlException ex)
        {
            throw new FormatException(
                string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Format_Invalid_XmlNotWellFormed, ex.Message),
                ex);
        }

        ValidateSchema(document, diagnostics);

        XElement root = document.Root ?? new XElement(s_ns + "NotableDateResource");

        string resourceId = (string?)root.Attribute("resourceId") ?? string.Empty;
        string schemaVersion = (string?)root.Attribute("schemaVersion") ?? string.Empty;

        ResolutionPolicy resolutionPolicy = ParseResolutionPolicy(root.Element(s_ns + "ResolutionPolicy"));
        List<AdjustmentPolicy> adjustmentPolicies = ParseAdjustmentPolicies(root.Element(s_ns + "AdjustmentPolicies"));
        List<NotableDateDefinition> notableDates = ParseNotableDates(root.Element(s_ns + "NotableDates"), diagnostics);
        List<NotableDateRuleOverride> overrides = ParseOverrides(root.Element(s_ns + "Overrides"), diagnostics);

        return new ParsedNotableDateDocument(resourceId, schemaVersion, resolutionPolicy, adjustmentPolicies, notableDates, overrides);
    }

    /// <summary>
    /// Validates the document against the embedded XSD, recording each schema violation as a diagnostic.
    /// </summary>
    /// <param name="document">The parsed XML document.</param>
    /// <param name="diagnostics">The collection that receives schema diagnostics.</param>
    private static void ValidateSchema(XDocument document, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        using Stream? schemaStream = typeof(NotableDateDocumentParser).Assembly.GetManifestResourceStream(SchemaResourceName);
        if (schemaStream is null)
            return;

        XmlSchemaSet schemas = new();
        using (XmlReader schemaReader = XmlReader.Create(schemaStream))
            schemas.Add(s_ns.NamespaceName, schemaReader);

        document.Validate(schemas, (sender, e) =>
        {
            NotableDateValidationSeverity severity = e.Severity == XmlSeverityType.Error
                ? NotableDateValidationSeverity.Error
                : NotableDateValidationSeverity.Warning;

            diagnostics.Add(new NotableDateValidationDiagnostic(
                severity,
                "BODU-CAL2-SCHEMA",
                string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Format_Invalid_XmlSchema, e.Message)));
        });
    }

    /// <summary>
    /// Parses the resource-level resolution policy, falling back to the recommended defaults when absent.
    /// </summary>
    /// <param name="element">The <c>ResolutionPolicy</c> element, or <see langword="null" />.</param>
    /// <returns>The parsed <see cref="ResolutionPolicy" />.</returns>
    private static ResolutionPolicy ParseResolutionPolicy(XElement? element)
    {
        if (element is null)
            return ResolutionPolicy.Default;

        return new ResolutionPolicy(
            ParseEnum(element.Attribute("duplicatePolicy")?.Value, DuplicatePolicy.Error),
            ParseEnum(element.Attribute("sameDayCollisionPolicy")?.Value, CollisionPolicy.KeepAll),
            ParseEnum(element.Attribute("spanCollisionPolicy")?.Value, CollisionPolicy.KeepAll),
            ParseEnum(element.Attribute("priorityDirection")?.Value, PriorityDirection.HigherWins),
            ParseEnum(element.Attribute("observedDateRangePolicy")?.Value, ObservedDateRangePolicy.ObservedOccurrenceControlsInclusion));
    }

    /// <summary>
    /// Parses the reusable adjustment policies declared by the resource.
    /// </summary>
    /// <param name="element">The <c>AdjustmentPolicies</c> element, or <see langword="null" />.</param>
    /// <returns>The parsed adjustment policies.</returns>
    private static List<AdjustmentPolicy> ParseAdjustmentPolicies(XElement? element)
    {
        List<AdjustmentPolicy> policies = new();
        if (element is null)
            return policies;

        foreach (XElement policy in element.Elements(s_ns + "AdjustmentPolicy"))
        {
            XElement? trigger = policy.Element(s_ns + "Trigger");
            XElement? action = policy.Element(s_ns + "Action");
            XElement? emission = policy.Element(s_ns + "Emission");

            IEnumerable<DayOfWeek> weekdays = trigger is null
                ? Array.Empty<DayOfWeek>()
                : trigger.Elements(s_ns + "Weekday").Select(w => ParseEnum(w.Attribute("value")?.Value, DayOfWeek.Monday));

            policies.Add(new AdjustmentPolicy(
                (string?)policy.Attribute("id") ?? string.Empty,
                ParseInt(policy.Attribute("priority")?.Value, 0),
                ParseScope(policy.Element(s_ns + "Scope")),
                ParseEnum(trigger?.Attribute("type")?.Value, AdjustmentTrigger.Always),
                weekdays,
                ParseEnum(action?.Attribute("type")?.Value, AdjustmentAction.None),
                ParseNullableEnum<DayOfWeek>(action?.Attribute("dayOfWeek")?.Value),
                ParseInt(action?.Attribute("days")?.Value, 0),
                ParseNullableInt(action?.Attribute("maxSearchDays")?.Value),
                ParseBool(action?.Attribute("skipWeekends")?.Value, true),
                ParseBool(action?.Attribute("skipNonWorkingDates")?.Value, false),
                ParseEnum(emission?.Attribute("mode")?.Value, EmissionMode.ActualOnly),
                (string?)emission?.Attribute("reason"),
                ParseNullableBool(emission?.Attribute("nonWorking")?.Value)));
        }

        return policies;
    }

    /// <summary>
    /// Parses an adjustment policy scope, falling back to a global scope when absent.
    /// </summary>
    /// <param name="element">The <c>Scope</c> element, or <see langword="null" />.</param>
    /// <returns>The parsed <see cref="AdjustmentScope" />.</returns>
    private static AdjustmentScope ParseScope(XElement? element)
    {
        if (element is null)
            return AdjustmentScope.Global;

        return new AdjustmentScope(
            element.Elements(s_ns + "Territory").Select(t => (string?)t.Attribute("code") ?? string.Empty),
            element.Elements(s_ns + "Calendar").Select(c => ParseEnum(c.Attribute("name")?.Value, CalendarSystem.Gregorian)),
            element.Elements(s_ns + "Category").Select(c => ParseEnum(c.Attribute("value")?.Value, NotableDateCategory.None)),
            element.Elements(s_ns + "NotableDate").Select(n => (string?)n.Attribute("ref") ?? string.Empty),
            element.Elements(s_ns + "Rule").Select(r => (string?)r.Attribute("ruleRef") ?? string.Empty));
    }

    /// <summary>
    /// Parses the notable-date concepts declared by the resource.
    /// </summary>
    /// <param name="element">The <c>NotableDates</c> element, or <see langword="null" />.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed notable-date concepts.</returns>
    private static List<NotableDateDefinition> ParseNotableDates(XElement? element, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        List<NotableDateDefinition> definitions = new();
        if (element is null)
            return definitions;

        foreach (XElement notableDate in element.Elements(s_ns + "NotableDate"))
        {
            string id = (string?)notableDate.Attribute("id") ?? string.Empty;

            definitions.Add(new NotableDateDefinition(
                id,
                (string?)notableDate.Attribute("displayName") ?? string.Empty,
                ParseEnum(notableDate.Attribute("category")?.Value, NotableDateCategory.None),
                ParseBool(notableDate.Attribute("defaultNonWorkingDay")?.Value, false),
                ParseInt(notableDate.Attribute("defaultDurationDays")?.Value, 1),
                ParseTags(notableDate.Element(s_ns + "Tags")),
                ParseRules(notableDate.Element(s_ns + "Rules"), id, diagnostics)));
        }

        return definitions;
    }

    /// <summary>
    /// Parses the rules belonging to a notable-date concept.
    /// </summary>
    /// <param name="element">The <c>Rules</c> element, or <see langword="null" />.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed rules.</returns>
    private static List<NotableDateRule> ParseRules(XElement? element, string notableDateId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        List<NotableDateRule> rules = new();
        if (element is null)
            return rules;

        foreach (XElement rule in element.Elements(s_ns + "Rule"))
            rules.Add(ParseRule(rule, notableDateId, diagnostics));

        return rules;
    }

    /// <summary>
    /// Parses a single rule element.
    /// </summary>
    /// <param name="element">The <c>Rule</c> element.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed <see cref="NotableDateRule" />.</returns>
    private static NotableDateRule ParseRule(XElement element, string notableDateId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        string id = (string?)element.Attribute("id") ?? string.Empty;
        RuleApplicability applicability = ParseApplicability(element.Element(s_ns + "Applicability"));

        return new NotableDateRule(
            id,
            ParseInt(element.Attribute("priority")?.Value, 0),
            ParseNullableEnum<NotableDateCategory>(element.Attribute("category")?.Value),
            ParseNullableBool(element.Attribute("nonWorking")?.Value),
            ParseNullableInt(element.Attribute("durationDays")?.Value),
            applicability,
            ParseStrategy(element.Element(s_ns + "Strategy"), applicability.Calendar, notableDateId, id, diagnostics),
            ParseAdjustments(element.Element(s_ns + "Adjustments")),
            ParseTags(element.Element(s_ns + "Tags")));
    }

    /// <summary>
    /// Parses a rule's applicability, falling back to a global Gregorian applicability when absent.
    /// </summary>
    /// <param name="element">The <c>Applicability</c> element, or <see langword="null" />.</param>
    /// <returns>The parsed <see cref="RuleApplicability" />.</returns>
    private static RuleApplicability ParseApplicability(XElement? element)
    {
        if (element is null)
            return new RuleApplicability(CalendarSystem.Gregorian, null, null, Array.Empty<string>(), Array.Empty<int>(), Array.Empty<int>());

        return new RuleApplicability(
            ParseEnum(element.Attribute("calendar")?.Value, CalendarSystem.Gregorian),
            ParseNullableInt(element.Attribute("fromYear")?.Value),
            ParseNullableInt(element.Attribute("toYear")?.Value),
            element.Elements(s_ns + "Territory").Select(t => (string?)t.Attribute("code") ?? string.Empty),
            element.Elements(s_ns + "OnlyYear").Select(y => ParseInt(y.Attribute("value")?.Value, 0)),
            element.Elements(s_ns + "ExceptYear").Select(y => ParseInt(y.Attribute("value")?.Value, 0)));
    }

    /// <summary>
    /// Parses a rule's strategy by dispatching on the single strategy child element.
    /// </summary>
    /// <param name="element">The <c>Strategy</c> element, or <see langword="null" />.</param>
    /// <param name="calendar">The calendar system the rule's applicability declares.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed <see cref="IDateCalculationStrategy" />.</returns>
    private static IDateCalculationStrategy ParseStrategy(XElement? element, CalendarSystem calendar, string notableDateId, string ruleId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        XElement? child = element?.Elements().FirstOrDefault();
        if (child is null)
            return new FixedDateStrategy(1, 1);

        switch (child.Name.LocalName)
        {
            case "Fixed":
            {
                (int month, string? monthAlias) = ParseFixedMonth(child.Attribute("month")?.Value, calendar, notableDateId, ruleId, diagnostics);
                return new FixedDateStrategy(
                    month,
                    Math.Clamp(ParseInt(child.Attribute("day")?.Value, 1), 1, 31),
                    calendar,
                    ParseBool(child.Attribute("sweepCalendarYears")?.Value, false),
                    ParseBool(child.Attribute("skipLeapMonth")?.Value, false),
                    monthAlias);
            }

            case "DayOfWeekInMonth":
                return new DayOfWeekInMonthStrategy(
                    ParseMonth(child.Attribute("month")?.Value, notableDateId, ruleId, diagnostics),
                    ParseEnum(child.Attribute("dayOfWeek")?.Value, DayOfWeek.Monday),
                    ParseEnum(child.Attribute("weekOrdinal")?.Value, WeekOrdinal.First));

            case "WeekdayNearDate":
                return new WeekdayNearDateStrategy(
                    ParseMonth(child.Attribute("month")?.Value, notableDateId, ruleId, diagnostics),
                    Math.Clamp(ParseInt(child.Attribute("day")?.Value, 1), 1, 31),
                    ParseEnum(child.Attribute("dayOfWeek")?.Value, DayOfWeek.Monday),
                    ParseEnum(child.Attribute("direction")?.Value, WeekdayProximity.OnOrAfter));

            case "RelativeWeekdayInMonth":
                return new RelativeWeekdayInMonthStrategy(
                    ParseMonth(child.Attribute("month")?.Value, notableDateId, ruleId, diagnostics),
                    ParseEnum(child.Attribute("dayOfWeek")?.Value, DayOfWeek.Monday),
                    ParseEnum(child.Attribute("weekOrdinal")?.Value, WeekOrdinal.First),
                    ParseEnum(child.Attribute("relativeDayOfWeek")?.Value, DayOfWeek.Monday),
                    ParseEnum(child.Attribute("direction")?.Value, WeekdayProximity.After));

            case "OffsetFromRule":
                return new OffsetFromRuleStrategy(
                    (string?)child.Attribute("notableDateRef") ?? string.Empty,
                    string.IsNullOrEmpty((string?)child.Attribute("ruleRef")) ? null : (string?)child.Attribute("ruleRef"),
                    ParseInt(child.Attribute("offsetDays")?.Value, 0));

            case "Algorithm":
                return new AlgorithmDateStrategy((string?)child.Attribute("key") ?? string.Empty);

            default:
                return new FixedDateStrategy(1, 1);
        }
    }

    /// <summary>
    /// Parses the adjustment references declared by a rule.
    /// </summary>
    /// <param name="element">The <c>Adjustments</c> element, or <see langword="null" />.</param>
    /// <returns>The referenced adjustment-policy identifiers.</returns>
    private static List<string> ParseAdjustments(XElement? element)
    {
        if (element is null)
            return new List<string>();

        return element.Elements(s_ns + "Adjustment")
            .Select(a => (string?)a.Attribute("policyRef") ?? string.Empty)
            .ToList();
    }

    /// <summary>
    /// Parses the tag values declared by a <c>Tags</c> element.
    /// </summary>
    /// <param name="element">The <c>Tags</c> element, or <see langword="null" />.</param>
    /// <returns>The tag values.</returns>
    private static List<string> ParseTags(XElement? element)
    {
        if (element is null)
            return new List<string>();

        return element.Elements(s_ns + "Tag")
            .Select(t => (string?)t.Attribute("value") ?? string.Empty)
            .ToList();
    }

    /// <summary>
    /// Parses the override operations declared by the resource.
    /// </summary>
    /// <param name="element">The <c>Overrides</c> element, or <see langword="null" />.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed override operations.</returns>
    private static List<NotableDateRuleOverride> ParseOverrides(XElement? element, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        List<NotableDateRuleOverride> overrides = new();
        if (element is null)
            return overrides;

        foreach (XElement operation in element.Elements())
        {
            string notableDateRef = (string?)operation.Attribute("notableDateRef") ?? string.Empty;

            if (operation.Name == s_ns + "RemoveRule")
            {
                overrides.Add(new RemoveRuleOverride(notableDateRef, (string?)operation.Attribute("ruleRef") ?? string.Empty));
            }
            else if (operation.Name == s_ns + "PatchRule")
            {
                overrides.Add(ParsePatchRule(operation, notableDateRef, diagnostics));
            }
            else if (operation.Name == s_ns + "AddRule")
            {
                XElement? ruleElement = operation.Element(s_ns + "Rule");
                if (ruleElement is not null)
                    overrides.Add(new AddRuleOverride(notableDateRef, ParseRule(ruleElement, notableDateRef, diagnostics)));
            }
        }

        return overrides;
    }

    /// <summary>
    /// Parses a <c>PatchRule</c> override operation.
    /// </summary>
    /// <param name="operation">The <c>PatchRule</c> element.</param>
    /// <param name="notableDateRef">The identifier of the targeted concept.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The parsed <see cref="PatchRuleOverride" />.</returns>
    private static PatchRuleOverride ParsePatchRule(XElement operation, string notableDateRef, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        string ruleRef = (string?)operation.Attribute("ruleRef") ?? string.Empty;

        XElement? applicabilityElement = operation.Element(s_ns + "Applicability");
        XElement? strategyElement = operation.Element(s_ns + "Strategy");
        XElement? adjustmentsElement = operation.Element(s_ns + "Adjustments");
        XElement? tagsElement = operation.Element(s_ns + "Tags");

        RuleApplicability? applicability = applicabilityElement is null ? null : ParseApplicability(applicabilityElement);

        return new PatchRuleOverride(
            notableDateRef,
            ruleRef,
            ParseNullableInt(operation.Attribute("priority")?.Value),
            ParseNullableEnum<NotableDateCategory>(operation.Attribute("category")?.Value),
            ParseNullableBool(operation.Attribute("nonWorking")?.Value),
            ParseNullableInt(operation.Attribute("durationDays")?.Value),
            applicability,
            strategyElement is null ? null : ParseStrategy(strategyElement, applicability?.Calendar ?? CalendarSystem.Gregorian, notableDateRef, ruleRef, diagnostics),
            adjustmentsElement is null ? null : ParseAdjustments(adjustmentsElement),
            tagsElement is null ? null : ParseTags(tagsElement));
    }

    /// <summary>
    /// Parses a month value expressed as a full English month name or an integer between 1 and 12.
    /// </summary>
    /// <param name="value">The raw month value.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>The one-based month, or 1 when the value is invalid.</returns>
    private static int ParseMonth(string? value, string notableDateId, string ruleId, ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (value is not null)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric) && numeric is >= 1 and <= 12)
                return numeric;

            int index = Array.FindIndex(s_monthNames, n => string.Equals(n, value, StringComparison.OrdinalIgnoreCase));
            if (index >= 0)
                return index + 1;
        }

        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            "BODU-CAL2-MONTH",
            string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Validation_InvalidMonthValue, notableDateId, ruleId, value ?? string.Empty)));

        return 1;
    }

    /// <summary>
    /// Parses a fixed-strategy month for a given calendar system, returning either a numeric month or a Hebrew alias
    /// whose number is resolved at calculation time.
    /// </summary>
    /// <param name="value">The raw month value.</param>
    /// <param name="calendar">The calendar system the month is expressed in.</param>
    /// <param name="notableDateId">The identifier of the owning concept, used in diagnostics.</param>
    /// <param name="ruleId">The identifier of the owning rule, used in diagnostics.</param>
    /// <param name="diagnostics">The collection that receives semantic diagnostics.</param>
    /// <returns>
    /// A tuple of the one-based month (or <c>0</c> when an alias supplies it) and an optional Hebrew month alias.
    /// </returns>
    private static (int Month, string? Alias) ParseFixedMonth(
        string? value,
        CalendarSystem calendar,
        string notableDateId,
        string ruleId,
        ICollection<NotableDateValidationDiagnostic> diagnostics)
    {
        if (calendar == CalendarSystem.Gregorian)
            return (ParseMonth(value, notableDateId, ruleId, diagnostics), null);

        if (value is not null)
        {
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numeric) && numeric is >= 1 and <= 13)
                return (numeric, null);

            switch (value)
            {
                case "Tishri": return (1, null);
                case "Heshvan": return (2, null);
                case "Kislev": return (3, null);
                case "Tevet": return (4, null);
                case "Shevat": return (5, null);
                case "AdarI": return (6, null);
                case "AdarII":
                case "LastAdar":
                case "Nisan":
                case "Iyar":
                case "Sivan":
                case "Tammuz":
                case "Av":
                case "Elul":
                    return (0, value);
                default:
                    break;
            }
        }

        diagnostics.Add(new NotableDateValidationDiagnostic(
            NotableDateValidationSeverity.Error,
            "BODU-CAL2-MONTH",
            string.Format(CultureInfo.InvariantCulture, Calendar2ResourceStrings.Validation_InvalidMonthValue, notableDateId, ruleId, value ?? string.Empty)));

        return (1, null);
    }

    /// <summary>
    /// Parses an enumeration value, falling back to a default when the value is absent or unrecognised.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The raw value.</param>
    /// <param name="fallback">The value returned when parsing fails.</param>
    /// <returns>The parsed enumeration value, or <paramref name="fallback" />.</returns>
    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum =>
        value is not null && Enum.TryParse(value, ignoreCase: true, out TEnum result) && Enum.IsDefined(result)
            ? result
            : fallback;

    /// <summary>
    /// Parses an optional enumeration value.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The raw value, or <see langword="null" />.</param>
    /// <returns>The parsed value, or <see langword="null" /> when absent or unrecognised.</returns>
    private static TEnum? ParseNullableEnum<TEnum>(string? value)
        where TEnum : struct, Enum =>
        value is not null && Enum.TryParse(value, ignoreCase: true, out TEnum result) && Enum.IsDefined(result)
            ? result
            : null;

    /// <summary>
    /// Parses an integer value, falling back to a default when parsing fails.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="fallback">The value returned when parsing fails.</param>
    /// <returns>The parsed integer, or <paramref name="fallback" />.</returns>
    private static int ParseInt(string? value, int fallback) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : fallback;

    /// <summary>
    /// Parses an optional integer value.
    /// </summary>
    /// <param name="value">The raw value, or <see langword="null" />.</param>
    /// <returns>The parsed integer, or <see langword="null" /> when absent or invalid.</returns>
    private static int? ParseNullableInt(string? value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : null;

    /// <summary>
    /// Parses a boolean value, falling back to a default when parsing fails.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <param name="fallback">The value returned when parsing fails.</param>
    /// <returns>The parsed boolean, or <paramref name="fallback" />.</returns>
    private static bool ParseBool(string? value, bool fallback)
    {
        if (value is null)
            return fallback;

        try
        {
            return XmlConvert.ToBoolean(value);
        }
        catch (FormatException)
        {
            return fallback;
        }
    }

    /// <summary>
    /// Parses an optional boolean value.
    /// </summary>
    /// <param name="value">The raw value, or <see langword="null" />.</param>
    /// <returns>The parsed boolean, or <see langword="null" /> when absent or invalid.</returns>
    private static bool? ParseNullableBool(string? value)
    {
        if (value is null)
            return null;

        try
        {
            return XmlConvert.ToBoolean(value);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
