// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilder.XmlRead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Implements the element-reading helpers that reconstruct a <see cref="NotableDateDocumentBuilder" /> from a parsed
/// XML document.
/// </summary>
public sealed partial class NotableDateDocumentBuilder
{
    /// <summary>
    /// Reads the <c>Metadata</c> element into the builder.
    /// </summary>
    /// <param name="element">The metadata element, or <see langword="null" /> when absent.</param>
    private void ReadMetadata(XElement? element)
    {
        if (element is null)
            return;

        _metadataName = (string?)element.Element(BuilderXml.Namespace + "Name");
        _metadataDescription = (string?)element.Element(BuilderXml.Namespace + "Description");
        _sources.Clear();
        foreach (XElement source in element.Elements(BuilderXml.Namespace + "Source"))
            _sources.Add(source.Value);
    }

    /// <summary>
    /// Reads the <c>ResolutionPolicy</c> element into the builder.
    /// </summary>
    /// <param name="element">The resolution-policy element, or <see langword="null" /> when absent.</param>
    private void ReadResolutionPolicy(XElement? element)
    {
        if (element is null)
            return;

        ResolutionPolicyBuilder policy = new();
        DayOfWeekSet? workingWeek = ParseWorkingWeek((string?)element.Attribute("workingDays"));

        XElement? precedenceElement = element.Element(BuilderXml.Namespace + "CategoryPrecedence");
        IReadOnlyList<NotableDateCategory>? categoryPrecedence = precedenceElement is null
            ? null
            : [.. precedenceElement.Elements(BuilderXml.Namespace + "Category")
                .Select(c => ParseEnum<NotableDateCategory>((string?)c.Attribute("value"), "categoryPrecedence"))];

        policy.SetParsedValues(
            ParseNullableEnum<DuplicatePolicy>((string?)element.Attribute("duplicatePolicy"), "duplicatePolicy"),
            ParseNullableEnum<CollisionPolicy>((string?)element.Attribute("sameDayCollisionPolicy"), "sameDayCollisionPolicy"),
            ParseNullableEnum<CollisionPolicy>((string?)element.Attribute("spanCollisionPolicy"), "spanCollisionPolicy"),
            ParseNullableEnum<PriorityDirection>((string?)element.Attribute("priorityDirection"), "priorityDirection"),
            ParseNullableEnum<ObservedDateRangePolicy>((string?)element.Attribute("observedDateRangePolicy"), "observedDateRangePolicy"),
            workingWeek,
            categoryPrecedence);

        _resolutionPolicy = policy;
    }

    /// <summary>
    /// Reads the <c>AdjustmentPolicies</c> element into the builder.
    /// </summary>
    /// <param name="element">The adjustment-policies element, or <see langword="null" /> when absent.</param>
    private void ReadAdjustmentPolicies(XElement? element)
    {
        if (element is null)
            return;

        foreach (XElement policyElement in element.Elements(BuilderXml.Namespace + "AdjustmentPolicy"))
            _adjustmentPolicies.Add(ReadAdjustmentPolicy(policyElement));
    }

    /// <summary>
    /// Reads a single <c>AdjustmentPolicy</c> element into a policy builder.
    /// </summary>
    /// <param name="element">The policy element.</param>
    /// <returns>The reconstructed <see cref="AdjustmentPolicyBuilder" />.</returns>
    private static AdjustmentPolicyBuilder ReadAdjustmentPolicy(XElement element)
    {
        AdjustmentPolicyBuilder policy = new((string?)element.Attribute("id") ?? string.Empty);
        policy.SetParsedHeader(
            BuilderXml.ParseInt((string?)element.Attribute("priority")),
            (string?)element.Attribute("description"),
            ReadScope(element.Element(BuilderXml.Namespace + "Scope")));

        XElement? trigger = element.Element(BuilderXml.Namespace + "Trigger");
        if (trigger is not null)
        {
            List<DayOfWeek> weekdays = new();
            foreach (XElement weekday in trigger.Elements(BuilderXml.Namespace + "Weekday"))
                weekdays.Add(ParseEnum<DayOfWeek>((string?)weekday.Attribute("value"), "trigger weekdays"));

            string? month = (string?)trigger.Attribute("month");
            policy.SetParsedTrigger(
                ParseNullableEnum<AdjustmentTrigger>((string?)trigger.Attribute("type"), "trigger type"),
                weekdays,
                string.IsNullOrEmpty(month) ? null : BuilderXml.ParseMonth(month),
                BuilderXml.ParseInt((string?)trigger.Attribute("day")),
                ParseNullableEnum<WeekOrdinal>((string?)trigger.Attribute("weekOrdinal"), "trigger weekOrdinal"),
                (string?)trigger.Attribute("handlerKey"));
        }

        XElement? action = element.Element(BuilderXml.Namespace + "Action");
        if (action is not null)
        {
            policy.SetParsedAction(
                ParseNullableEnum<AdjustmentAction>((string?)action.Attribute("type"), "action type"),
                BuilderXml.ParseInt((string?)action.Attribute("days")),
                ParseNullableEnum<DayOfWeek>((string?)action.Attribute("dayOfWeek"), "action dayOfWeek"),
                BuilderXml.ParseInt((string?)action.Attribute("maxSearchDays")),
                BuilderXml.ParseBool((string?)action.Attribute("skipWeekends")),
                BuilderXml.ParseBool((string?)action.Attribute("skipNonWorkingDates")),
                (string?)action.Attribute("notableDateRef"),
                (string?)action.Attribute("ruleRef"),
                (string?)action.Attribute("handlerKey"));
        }

        XElement? emission = element.Element(BuilderXml.Namespace + "Emission");
        if (emission is not null)
        {
            policy.SetParsedEmission(
                ParseNullableEnum<EmissionMode>((string?)emission.Attribute("mode"), "emission mode"),
                (string?)emission.Attribute("reason"),
                BuilderXml.ParseBool((string?)emission.Attribute("nonWorking")));
        }

        XElement? parameters = element.Element(BuilderXml.Namespace + "Parameters");
        if (parameters is not null)
        {
            List<KeyValuePair<string, string>> pairs = new();
            foreach (XElement param in parameters.Elements(BuilderXml.Namespace + "Param"))
                pairs.Add(new KeyValuePair<string, string>((string?)param.Attribute("key") ?? string.Empty, (string?)param.Attribute("value") ?? string.Empty));

            policy.SetParsedParameters(pairs);
        }

        return policy;
    }

    /// <summary>
    /// Reads an adjustment-policy <c>Scope</c> element into a scope builder.
    /// </summary>
    /// <param name="element">The scope element, or <see langword="null" /> when absent.</param>
    /// <returns>The reconstructed scope builder, or <see langword="null" /> when the element is absent.</returns>
    private static AdjustmentScopeBuilder? ReadScope(XElement? element)
    {
        if (element is null)
            return null;

        List<string> territories = new();
        List<CalendarSystem> calendars = new();
        List<NotableDateCategory> categories = new();
        List<string> notableDateRefs = new();
        List<(string NotableDateRef, string RuleRef)> ruleRefs = new();
        List<int> onlyYears = new();
        List<int> exceptYears = new();

        foreach (XElement child in element.Elements())
        {
            switch (child.Name.LocalName)
            {
                case "Territory":
                    territories.Add((string?)child.Attribute("code") ?? string.Empty);
                    break;

                case "Calendar":
                    calendars.Add(ParseEnum<CalendarSystem>((string?)child.Attribute("name"), "scope calendars"));
                    break;

                case "Category":
                    categories.Add(ParseEnum<NotableDateCategory>((string?)child.Attribute("value"), "scope categories"));
                    break;

                case "NotableDate":
                    notableDateRefs.Add((string?)child.Attribute("ref") ?? string.Empty);
                    break;

                case "Rule":
                    ruleRefs.Add(((string?)child.Attribute("notableDateRef") ?? string.Empty, (string?)child.Attribute("ruleRef") ?? string.Empty));
                    break;

                case "OnlyYear":
                    onlyYears.Add(BuilderXml.ParseInt((string?)child.Attribute("value")) ?? 0);
                    break;

                case "ExceptYear":
                    exceptYears.Add(BuilderXml.ParseInt((string?)child.Attribute("value")) ?? 0);
                    break;

                default:
                    break;
            }
        }

        AdjustmentScopeBuilder scope = new();
        scope.SetParsedValues(
            territories,
            calendars,
            categories,
            notableDateRefs,
            ruleRefs,
            onlyYears,
            exceptYears,
            BuilderXml.ParseInt((string?)element.Attribute("fromYear")),
            BuilderXml.ParseInt((string?)element.Attribute("toYear")));

        return scope;
    }

    /// <summary>
    /// Reads the <c>Imports</c> element into the builder.
    /// </summary>
    /// <param name="element">The imports element, or <see langword="null" /> when absent.</param>
    private void ReadImports(XElement? element)
    {
        if (element is null)
            return;

        foreach (XElement importElement in element.Elements(BuilderXml.Namespace + "Import"))
        {
            ImportBuilder import = new((string?)importElement.Attribute("resource") ?? string.Empty);
            foreach (XElement useElement in importElement.Elements(BuilderXml.Namespace + "Use"))
                import.AddUse(ReadImportUse(useElement));

            _imports.Add(import);
        }
    }

    /// <summary>
    /// Reads a single import <c>Use</c> element into an import-use builder.
    /// </summary>
    /// <param name="element">The use element.</param>
    /// <returns>The reconstructed import-use builder.</returns>
    private static ImportUseBuilder ReadImportUse(XElement element)
    {
        List<string> adjustments = new();
        XElement? adjustmentsElement = element.Element(BuilderXml.Namespace + "Adjustments");
        if (adjustmentsElement is not null)
        {
            foreach (XElement adjustment in adjustmentsElement.Elements(BuilderXml.Namespace + "Adjustment"))
                adjustments.Add((string?)adjustment.Attribute("policyRef") ?? string.Empty);
        }

        ImportUseBuilder use = new((string?)element.Attribute("notableDateRef") ?? string.Empty);
        use.SetParsedValues(
            (string?)element.Attribute("as"),
            (string?)element.Attribute("territory"),
            ParseNullableEnum<NotableDateCategory>((string?)element.Attribute("category"), "import use category"),
            BuilderXml.ParseBool((string?)element.Attribute("nonWorking")),
            adjustments);

        return use;
    }

    /// <summary>
    /// Reads the <c>NotableDates</c> element into the builder.
    /// </summary>
    /// <param name="element">The notable-dates element, or <see langword="null" /> when absent.</param>
    private void ReadNotableDates(XElement? element)
    {
        if (element is null)
            return;

        foreach (XElement definitionElement in element.Elements(BuilderXml.Namespace + "NotableDate"))
            _definitions.Add(ReadNotableDate(definitionElement));
    }

    /// <summary>
    /// Reads a single <c>NotableDate</c> element into a concept builder.
    /// </summary>
    /// <param name="element">The concept element.</param>
    /// <returns>The reconstructed concept builder.</returns>
    private static NotableDateDefinitionBuilder ReadNotableDate(XElement element)
    {
        NotableDateDefinitionBuilder definition = new(
            (string?)element.Attribute("id") ?? string.Empty,
            (string?)element.Attribute("displayName") ?? string.Empty,
            ParseEnum<NotableDateCategory>((string?)element.Attribute("category"), "category"));

        definition.SetParsedValues(
            BuilderXml.ParseInt((string?)element.Attribute("defaultDurationDays")),
            BuilderXml.ParseBool((string?)element.Attribute("defaultNonWorkingDay")),
            ReadTags(element.Element(BuilderXml.Namespace + "Tags")));

        XElement? rules = element.Element(BuilderXml.Namespace + "Rules");
        if (rules is not null)
        {
            foreach (XElement ruleElement in rules.Elements(BuilderXml.Namespace + "Rule"))
                definition.AddRule(ReadRule(ruleElement));
        }

        return definition;
    }

    /// <summary>
    /// Reads a single <c>Rule</c> or <c>PatchRule</c> element into a rule builder, capturing its identifier from the
    /// supplied attribute.
    /// </summary>
    /// <param name="element">The rule element.</param>
    /// <param name="idAttributeName">The name of the attribute carrying the rule identifier.</param>
    /// <returns>The reconstructed rule builder.</returns>
    private static NotableDateRuleBuilder ReadRule(XElement element, string idAttributeName = "id")
    {
        NotableDateRuleBuilder rule = new((string?)element.Attribute(idAttributeName) ?? string.Empty);

        XElement? applicability = element.Element(BuilderXml.Namespace + "Applicability");
        rule.SetParsedScalars(
            BuilderXml.ParseInt((string?)element.Attribute("priority")),
            ParseNullableEnum<NotableDateCategory>((string?)element.Attribute("category"), "category"),
            BuilderXml.ParseBool((string?)element.Attribute("nonWorking")),
            BuilderXml.ParseInt((string?)element.Attribute("durationDays")),
            (string?)element.Attribute("comment"),
            applicability is null ? null : ParseNullableEnum<CalendarSystem>((string?)applicability.Attribute("calendar"), "calendar"),
            applicability is null ? null : BuilderXml.ParseInt((string?)applicability.Attribute("fromYear")),
            applicability is null ? null : BuilderXml.ParseInt((string?)applicability.Attribute("toYear")),
            applicability is null ? null : BuilderXml.ParseInt((string?)applicability.Attribute("everyYears")),
            applicability is null ? null : BuilderXml.ParseInt((string?)applicability.Attribute("anchorYear")));

        List<string> territories = new();
        List<int> onlyYears = new();
        List<int> exceptYears = new();
        if (applicability is not null)
        {
            foreach (XElement territory in applicability.Elements(BuilderXml.Namespace + "Territory"))
                territories.Add((string?)territory.Attribute("code") ?? string.Empty);
            foreach (XElement year in applicability.Elements(BuilderXml.Namespace + "OnlyYear"))
                onlyYears.Add(BuilderXml.ParseInt((string?)year.Attribute("value")) ?? 0);
            foreach (XElement year in applicability.Elements(BuilderXml.Namespace + "ExceptYear"))
                exceptYears.Add(BuilderXml.ParseInt((string?)year.Attribute("value")) ?? 0);
        }

        List<string> adjustments = new();
        XElement? adjustmentsElement = element.Element(BuilderXml.Namespace + "Adjustments");
        if (adjustmentsElement is not null)
        {
            foreach (XElement adjustment in adjustmentsElement.Elements(BuilderXml.Namespace + "Adjustment"))
                adjustments.Add((string?)adjustment.Attribute("policyRef") ?? string.Empty);
        }

        rule.SetParsedCollections(territories, onlyYears, exceptYears, ReadTags(element.Element(BuilderXml.Namespace + "Tags")), adjustments);

        XElement? strategy = element.Element(BuilderXml.Namespace + "Strategy");
        XElement? strategyChild = strategy?.Elements().FirstOrDefault();
        rule.SetParsedStrategy(strategyChild is null ? null : new XElement(strategyChild));

        if (element.Element(BuilderXml.Namespace + "Recurrence") is XElement recurrence)
            rule.SetParsedRecurrence(new XElement(recurrence));

        if (element.Element(BuilderXml.Namespace + "Duration")?.Element(BuilderXml.Namespace + "UntilDate") is XElement untilDate)
        {
            XElement? endStrategyChild = untilDate.Element(BuilderXml.Namespace + "Strategy")?.Elements().FirstOrDefault();
            rule.SetParsedDuration(
                endStrategyChild is null ? null : new XElement(endStrategyChild),
                ParseNullableEnum<DateBoundary>((string?)untilDate.Attribute("startBoundary"), "untilDate startBoundary") ?? DateBoundary.Inclusive,
                ParseNullableEnum<DateBoundary>((string?)untilDate.Attribute("endBoundary"), "untilDate endBoundary") ?? DateBoundary.Inclusive,
                ParseNullableEnum<EndDateSelection>((string?)untilDate.Attribute("selection"), "untilDate selection") ?? EndDateSelection.FirstOnOrAfterStart);
        }

        return rule;
    }

    /// <summary>
    /// Reads the <c>Overrides</c> element into the builder.
    /// </summary>
    /// <param name="element">The overrides element, or <see langword="null" /> when absent.</param>
    private void ReadOverrides(XElement? element)
    {
        if (element is null)
            return;

        OverrideBuilder overrides = new();
        foreach (XElement child in element.Elements())
        {
            switch (child.Name.LocalName)
            {
                case "AddRule":
                    overrides.AddEntry(new OverrideEntry(
                        OverrideOperation.AddRule,
                        (string?)child.Attribute("notableDateRef") ?? string.Empty,
                        null,
                        ReadRule(child.Element(BuilderXml.Namespace + "Rule") ?? new XElement(BuilderXml.Namespace + "Rule"))));
                    break;

                case "RemoveRule":
                    overrides.AddEntry(new OverrideEntry(
                        OverrideOperation.RemoveRule,
                        (string?)child.Attribute("notableDateRef") ?? string.Empty,
                        (string?)child.Attribute("ruleRef") ?? string.Empty,
                        null));
                    break;

                case "PatchRule":
                    overrides.AddEntry(new OverrideEntry(
                        OverrideOperation.PatchRule,
                        (string?)child.Attribute("notableDateRef") ?? string.Empty,
                        (string?)child.Attribute("ruleRef") ?? string.Empty,
                        ReadRule(child, "ruleRef")));
                    break;

                default:
                    break;
            }
        }

        _overrides = overrides;
    }

    /// <summary>
    /// Reads a <c>Tags</c> element into a list of tag values.
    /// </summary>
    /// <param name="element">The tags element, or <see langword="null" /> when absent.</param>
    /// <returns>The tag values; empty when the element is absent.</returns>
    private static List<string> ReadTags(XElement? element)
    {
        List<string> tags = new();
        if (element is null)
            return tags;

        foreach (XElement tag in element.Elements(BuilderXml.Namespace + "Tag"))
            tags.Add((string?)tag.Attribute("value") ?? string.Empty);

        return tags;
    }

    /// <summary>
    /// Parses an enumeration value by member name, as the loader does, returning the enumeration's default when the
    /// value is absent.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The string form, or <see langword="null" /> when absent.</param>
    /// <param name="field">The name of the field the value belongs to, used in the exception message.</param>
    /// <returns>The named member, or the enumeration's default when the value is absent.</returns>
    /// <exception cref="FormatException">
    /// <paramref name="value" /> names no member of <typeparamref name="TEnum" />.
    /// </exception>
    private static TEnum ParseEnum<TEnum>(string? value, string field)
        where TEnum : struct, Enum =>
        ParseNullableEnum<TEnum>(value, field) ?? default;

    /// <summary>
    /// Parses an optional enumeration value by member name, ignoring case, with the loader's rules: a number, a
    /// comma-separated list, or a name surrounded by white space names no member.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration type.</typeparam>
    /// <param name="value">The string form, or <see langword="null" /> when absent.</param>
    /// <param name="field">The name of the field the value belongs to, used in the exception message.</param>
    /// <returns>The named member, or <see langword="null" /> when the value is absent.</returns>
    /// <exception cref="FormatException">
    /// <paramref name="value" /> names no member of <typeparamref name="TEnum" />.
    /// </exception>
    private static TEnum? ParseNullableEnum<TEnum>(string? value, string field)
        where TEnum : struct, Enum
    {
        List<NotableDateValidationDiagnostic> diagnostics = new();
        TEnum? result = NotableDateDocumentParseHelpers.ParseNullableEnum<TEnum>(value, field, diagnostics);
        ThrowIfMalformed(diagnostics);

        return result;
    }

    /// <summary>
    /// Parses an optional working week with the loader's rules: only the seven-character, Sunday-first binary pattern
    /// is a working week, and a blank value is absent.
    /// </summary>
    /// <param name="value">The <c>workingDays</c> value, or <see langword="null" /> when absent.</param>
    /// <returns>The working week, or <see langword="null" /> when the value is absent or blank.</returns>
    /// <exception cref="FormatException"><paramref name="value" /> is not a binary working-week pattern.</exception>
    private static DayOfWeekSet? ParseWorkingWeek(string? value)
    {
        List<NotableDateValidationDiagnostic> diagnostics = new();
        DayOfWeekSet? result = NotableDateDocumentParseHelpers.ParseWorkingWeek(value, diagnostics);
        ThrowIfMalformed(diagnostics);

        return result;
    }

    /// <summary>
    /// Throws when the loader's parse rules recorded a diagnostic for a value, so the builder rejects exactly the
    /// values the loader rejects, with the loader's message.
    /// </summary>
    /// <param name="diagnostics">The diagnostics the loader's parse rules recorded for one value.</param>
    /// <exception cref="FormatException"><paramref name="diagnostics" /> holds a diagnostic.</exception>
    private static void ThrowIfMalformed(List<NotableDateValidationDiagnostic> diagnostics)
    {
        if (diagnostics.Count > 0) throw new FormatException(diagnostics[0].Message);
    }
}
