// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleParser.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Parses authored XML payloads into <see cref="NotableDateRule" /> instances after schema validation.
/// </summary>
/// <remarks>
/// <para>
/// XML inputs are validated against the embedded <c>NotableDates.xsd</c> schema before parsing. Schema violations surface as
/// <see cref="XmlSchemaValidationException" /> so authoring errors are caught at parse time rather than at first query. The JSON
/// authoring counterpart lives in <see cref="NotableDateRuleJsonParser" /> and produces the same
/// <see cref="ParsedNotableDateDocument" /> output, so downstream loaders can treat both source formats uniformly.
/// </para>
/// <para>
/// The parser is the bottom of the rule-loading stack: it converts authored markup into the in-memory rule objects that
/// <see cref="XmlResourceNotableDateRuleProvider" /> assembles into a flat rule set, that <see cref="NotableDateService" /> then
/// resolves into <see cref="NotableDate" /> values. Use the <see cref="ParseXml(string)" /> overloads when only the local rules
/// matter; use <see cref="ParseDocument(string)" /> when <c>&lt;Use&gt;</c> cherry-pick directives must also be inspected (for
/// example to walk a graph of imported resources).
/// </para>
/// <para>
/// This class replaces <c>NotableDateDefinitionParser</c>. The new schema vocabulary uses <c>Rule</c> as the per-definition element and
/// names the strategy child elements <c>Fixed</c>, <c>DayOfWeekInMonth</c>, <c>OffsetFromAnchor</c>, and <c>Algorithm</c>.
/// </para>
/// </remarks>
/// <example>
/// <para>Parse an inline XML payload into a list of <see cref="NotableDateRule" /> instances:</para>
/// <code>
/// const string xml = """
///     &lt;NotableDates xmlns="urn:bodu:globalization:calendar"&gt;
///       &lt;Rule name="Australia Day" category="Public" territoryCode="AU" isNonWorkingDay="true"&gt;
///         &lt;Fixed month="1" day="26" /&gt;
///         &lt;Adjustment key="weekend-roll"
///                     trigger="IfWeekend"
///                     action="MoveToNextMonday"
///                     isNonWorkingDay="true" /&gt;
///       &lt;/Rule&gt;
///       &lt;Rule name="Easter Sunday" category="Religious"&gt;
///         &lt;Algorithm key="easter-sunday" /&gt;
///       &lt;/Rule&gt;
///     &lt;/NotableDates&gt;
///     """;
///
/// List&lt;NotableDateRule&gt; rules = NotableDateRuleParser.ParseXml(xml);
/// // rules[0] is the Fixed Australia Day rule with one weekend-roll adjustment.
/// // rules[1] is the Algorithm-backed Easter Sunday rule.
///
/// // When &lt;Use&gt; directives also matter, parse the full document instead:
/// ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
/// IReadOnlyList&lt;NotableDateRuleUseGroup&gt; imports = document.UseGroups;
/// </code>
/// </example>
public static class NotableDateRuleParser
{
    private static readonly XNamespace s_namespace = "urn:bodu:globalization:calendar";
    private static readonly XmlSchemaSet s_schemaSet = LoadSchema();

    /// <summary>
    /// Parses the supplied XML string into rules after validating against the embedded schema.
    /// </summary>
    /// <param name="xml">The XML payload. Must not be <see langword="null" /> or whitespace.</param>
    /// <returns>The parsed rules.</returns>
    /// <remarks>
    /// This convenience overload returns only the rules and discards any <c>Import</c> or <c>Suppress</c> directives. To resolve a
    /// document graph including imports, call <see cref="ParseDocument(string)" /> instead and feed the result to a loader such as
    /// <see cref="XmlResourceNotableDateRuleProvider" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xml" /> is <see langword="null" />, empty, or whitespace.</exception>
    /// <exception cref="XmlSchemaValidationException">Thrown when the XML does not conform to the embedded schema.</exception>
    public static List<NotableDateRule> ParseXml(string xml) => [.. ParseDocument(xml).LocalRules];

    /// <summary>
    /// Parses the supplied <see cref="XDocument" /> into rules after validating against the embedded schema.
    /// </summary>
    /// <param name="document">The XML document. Must not be <see langword="null" />.</param>
    /// <returns>The parsed rules.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document" /> is <see langword="null" />.</exception>
    public static List<NotableDateRule> ParseXml(XDocument document) => [.. ParseDocument(document).LocalRules];

    /// <summary>
    /// Parses the supplied XML string into a <see cref="ParsedNotableDateDocument" />, exposing local rules together with any
    /// <c>Import</c> and <c>Suppress</c> directives.
    /// </summary>
    /// <param name="xml">The XML payload. Must not be <see langword="null" /> or whitespace.</param>
    /// <returns>The parsed document, including imports, suppressions, and rules.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="xml" /> is <see langword="null" />, empty, or whitespace.</exception>
    /// <exception cref="XmlSchemaValidationException">Thrown when the XML does not conform to the embedded schema.</exception>
    public static ParsedNotableDateDocument ParseDocument(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            throw new ArgumentNullException(nameof(xml));

        using var stringReader = new StringReader(xml);
        using var xmlReader = XmlReader.Create(stringReader, CreateValidationSettings());
        var document = XDocument.Load(xmlReader);
        return ParseDocumentInternal(document);
    }

    /// <summary>
    /// Parses the supplied <see cref="XDocument" /> into a <see cref="ParsedNotableDateDocument" />.
    /// </summary>
    /// <param name="document">The XML document. Must not be <see langword="null" />.</param>
    /// <returns>The parsed document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document" /> is <see langword="null" />.</exception>
    public static ParsedNotableDateDocument ParseDocument(XDocument document)
    {
        if (document is null)
            throw new ArgumentNullException(nameof(document));

        ValidateDocument(document);
        return ParseDocumentInternal(document);
    }

    // ----------------------------------------------------------------------------
    // Per-element parsing
    // ----------------------------------------------------------------------------

    /// <summary>
    /// Parses a validated <see cref="XDocument" /> into a <see cref="ParsedNotableDateDocument" />,
    /// materializing each &lt;NotableDate&gt; child into zero or more <see cref="NotableDateRule" />
    /// instances along with any &lt;Use&gt; group references.
    /// </summary>
    /// <param name="document">The notable-date XML document to parse; must already be schema-validated.</param>
    /// <returns>The parsed document model.</returns>
    private static ParsedNotableDateDocument ParseDocumentInternal(XDocument document)
    {
        var useGroups = document.Descendants(s_namespace + "UseFrom")
            .Select(ParseUseGroup)
            .ToImmutableArray();

        var rules = document.Descendants(s_namespace + "NotableDate")
            .SelectMany(ParseNotableDate)
            .ToImmutableArray();

        return new ParsedNotableDateDocument(useGroups, rules);
    }

    /// <summary>
    /// Parses a &lt;UseFrom&gt; element into a <see cref="NotableDateRuleUseGroup" />, enumerating
    /// all child &lt;Use&gt; directives.
    /// </summary>
    /// <param name="useFromElement">The &lt;UseFrom&gt; XML element.</param>
    /// <returns>The parsed use-group instance.</returns>
    private static NotableDateRuleUseGroup ParseUseGroup(XElement useFromElement)
    {
        var resource = GetRequiredAttribute(useFromElement, "resource");
        var useAll = useFromElement.Element(s_namespace + "UseAll") is not null;

        var uses = useFromElement.Elements(s_namespace + "Use")
            .Select(ParseUseDirective)
            .ToImmutableArray();

        return new NotableDateRuleUseGroup(resource, useAll, uses);
    }

    /// <summary>
    /// Parses a single &lt;Use&gt; directive element into a
    /// <see cref="NotableDateRuleUseDirective" />, including the optional nested &lt;Rule&gt;
    /// override body and the <c>clearTags</c> / <c>clearAdjustments</c> flags.
    /// </summary>
    /// <param name="useElement">The &lt;Use&gt; XML element.</param>
    /// <returns>The parsed use directive.</returns>
    private static NotableDateRuleUseDirective ParseUseDirective(XElement useElement)
    {
        XElement? overrideRuleElement = useElement.Element(s_namespace + "Rule");
        NotableDateRuleOverrideBody? overrideBody = overrideRuleElement is null ? null : ParseOverrideBody(overrideRuleElement);

        return new NotableDateRuleUseDirective(
            SourceRuleName: GetRequiredAttribute(useElement, "name"),
            LocalName: GetOptionalAttribute(useElement, "as"),
            Category: ParseOptionalEnum<NotableDateCategory>(useElement, "category"),
            TerritoryCode: GetOptionalAttribute(useElement, "territory"),
            IsNonWorkingDay: ParseOptionalBool(useElement, "nonWorking"),
            FirstYear: ParseOptionalInt(useElement, "firstYear"),
            LastYear: ParseOptionalInt(useElement, "lastYear"),
            OccurrenceYears: ParseOptionalInt(useElement, "occurrenceYears"),
            DurationDays: ParseOptionalInt(useElement, "durationDays"),
            Priority: ParseOptionalInt(useElement, "priority"),
            Comment: GetOptionalAttribute(useElement, "comment"),
            ClearTags: ParseOptionalBool(useElement, "clearTags") ?? false,
            ClearAdjustments: ParseOptionalBool(useElement, "clearAdjustments") ?? false,
            ClearInherited: ParseOptionalBool(useElement, "clearInherited") ?? false,
            OverrideBody: overrideBody);
    }

    /// <summary>
    /// Parses the nested &lt;Rule&gt; override body inside a &lt;Use&gt; directive into a
    /// <see cref="NotableDateRuleOverrideBody" />.
    /// </summary>
    /// <param name="ruleElement">The nested &lt;Rule&gt; XML element.</param>
    /// <returns>The parsed override body.</returns>
    /// <remarks>
    /// The nested form is structurally identical to a standalone &lt;Rule&gt; but every child
    /// element is optional: the strategy may be absent (inherit the source strategy), and Tag
    /// and Adjustment children are accumulated for merging by the flatten pipeline.
    /// </remarks>
    private static NotableDateRuleOverrideBody ParseOverrideBody(XElement ruleElement)
    {
        XElement? strategyElement = ruleElement.Elements()
            .FirstOrDefault(e => IsStrategyElement(e.Name.LocalName));

        DateResolutionStrategy? strategy = strategyElement is null
            ? null
            : strategyElement.Name.LocalName switch
            {
                "Fixed" => DateResolutionStrategy.Fixed,
                "DayOfWeekInMonth" => DateResolutionStrategy.DayOfWeekInMonth,
                "Algorithm" => DateResolutionStrategy.Algorithm,
                "OffsetFromAnchor" => DateResolutionStrategy.OffsetFromAnchor,
                _ => throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_UnknownStrategyElementOnOverrideRule, strategyElement.Name.LocalName))
            };

        // The inner <Rule>'s name attribute is treated as a rule-level identifier (RuleName), used by the merger to target a
        // specific inherited rule when the source notable date contains more than one. It does not rename the inherited rule —
        // canonical naming flows from the source rule or the directive's `as` attribute.
        var body = new NotableDateRuleOverrideBody
        {
            RuleName = GetOptionalAttribute(ruleElement, "name"),
            Category = ParseOptionalEnum<NotableDateCategory>(ruleElement, "category"),
            TerritoryCode = GetOptionalAttribute(ruleElement, "territory"),
            IsNonWorkingDay = ParseOptionalBool(ruleElement, "nonWorking"),
            FirstYear = ParseOptionalInt(ruleElement, "firstYear"),
            LastYear = ParseOptionalInt(ruleElement, "lastYear"),
            OccurrenceYears = ParseOptionalInt(ruleElement, "occurrenceYears"),
            DurationDays = ParseOptionalInt(ruleElement, "durationDays"),
            Priority = ParseOptionalInt(ruleElement, "priority"),
            Comment = GetOptionalAttribute(ruleElement, "comment"),
            CalendarType = ParseOptionalType<SysGlobal.Calendar>(ruleElement, "calendarType"),
            Strategy = strategy,
            Tags = [.. ruleElement.Elements(s_namespace + "Tag")
                .Select(t => t.Value)
                .Where(t => !string.IsNullOrWhiteSpace(t))],
            Adjustments = [.. ruleElement.Elements(s_namespace + "Adjustment").Select(ParseAdjustment)],
        };

        if (strategyElement is not null)
            body = ApplyStrategySpecificsToBody(body, strategyElement);

        EnsureUniqueAdjustmentKeys(body.Adjustments, ruleElement);
        return body;
    }

    /// <summary>
    /// Applies the strategy-specific attributes on <paramref name="strategyElement" /> to the
    /// override body. Mirrors <see cref="ApplyStrategySpecifics(NotableDateRule, XElement)" />
    /// but writes to a <see cref="NotableDateRuleOverrideBody" />.
    /// </summary>
    /// <param name="body">The override body receiving strategy-specific fields.</param>
    /// <param name="strategyElement">The XML element describing the strategy.</param>
    /// <returns>The override body with strategy-specific fields populated.</returns>
    private static NotableDateRuleOverrideBody ApplyStrategySpecificsToBody(NotableDateRuleOverrideBody body, XElement strategyElement)
    {
        if (body.Strategy == DateResolutionStrategy.Fixed)
        {
            (var monthNum, var monthAlias) = ParseMonthToken(GetRequiredAttribute(strategyElement, "month"));
            return body with
            {
                Month = monthNum,
                CalendarMonthAlias = monthAlias,
                Day = int.Parse(GetRequiredAttribute(strategyElement, "day"), CultureInfo.InvariantCulture),
                SkipLeapMonth = ParseOptionalBool(strategyElement, "skipLeapMonth") ?? false,
                SweepCalendarYears = ParseOptionalBool(strategyElement, "sweepCalendarYears") ?? false,
            };
        }

        return body.Strategy switch
        {
            DateResolutionStrategy.DayOfWeekInMonth => body with
            {
                Month = ParseMonth(GetRequiredAttribute(strategyElement, "month")),
                DayOfWeek = ParseRequiredEnum<DayOfWeek>(strategyElement, "dayOfWeek"),
                WeekOrdinal = ParseRequiredEnum<WeekOfMonthOrdinal>(strategyElement, "weekOrdinal"),
            },
            DateResolutionStrategy.OffsetFromAnchor => body with
            {
                AnchorRuleName = GetRequiredAttribute(strategyElement, "name"),
                OffsetDays = int.Parse(GetRequiredAttribute(strategyElement, "offset"), CultureInfo.InvariantCulture),
            },
            DateResolutionStrategy.Algorithm => body with
            {
                AlgorithmKey = GetOptionalAttribute(strategyElement, "key"),
                AlgorithmType = ParseOptionalType<INotableDateAlgorithm>(strategyElement, "type"),
                AlgorithmMonth = GetOptionalAttribute(strategyElement, "month"),
                AlgorithmDay = ParseOptionalInt(strategyElement, "day"),
            },
            _ => body,
        };
    }

    /// <summary>
    /// Enforces the per-rule uniqueness invariant on adjustment keys — the same invariant the
    /// merge pipeline relies on when deciding between replace and append semantics.
    /// </summary>
    /// <param name="adjustments">The adjustments whose keys are being validated.</param>
    /// <param name="contextElement">The owning XML element used in diagnostic messages.</param>
    private static void EnsureUniqueAdjustmentKeys(ImmutableArray<ObservanceAdjustment> adjustments, XElement contextElement)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ObservanceAdjustment adjustment in adjustments)
        {
            if (!seen.Add(adjustment.Key))
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_DuplicateAdjustmentKey, adjustment.Key, contextElement.Name.LocalName));
        }
    }

    /// <summary>
    /// Expands a single &lt;NotableDate&gt; XML element into its one-or-more
    /// <see cref="NotableDateRule" /> instances, applying each strategy element's specifics.
    /// </summary>
    /// <param name="notableDateElement">The &lt;NotableDate&gt; XML element.</param>
    /// <returns>The sequence of rules derived from the element.</returns>
    private static IEnumerable<NotableDateRule> ParseNotableDate(XElement notableDateElement)
    {
        var name = GetRequiredAttribute(notableDateElement, "name");

        foreach (XElement ruleElement in notableDateElement.Elements(s_namespace + "Rule"))
        {
            XElement strategyElement = ruleElement.Elements()
                .FirstOrDefault(e => IsStrategyElement(e.Name.LocalName))
                ?? throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_RuleMissingStrategy, name));

            DateResolutionStrategy strategy = strategyElement.Name.LocalName switch
            {
                "Fixed" => DateResolutionStrategy.Fixed,
                "DayOfWeekInMonth" => DateResolutionStrategy.DayOfWeekInMonth,
                "Algorithm" => DateResolutionStrategy.Algorithm,
                "OffsetFromAnchor" => DateResolutionStrategy.OffsetFromAnchor,
                _ => throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_UnknownStrategyElementOnRule, strategyElement.Name.LocalName, name))
            };

            var adjustments = ruleElement.Elements(s_namespace + "Adjustment")
                .Select(ParseAdjustment)
                .ToImmutableArray();

            EnsureUniqueAdjustmentKeys(adjustments, ruleElement);

            var rule = new NotableDateRule
            {
                Name = name,
                RuleName = GetOptionalAttribute(ruleElement, "name"),
                Strategy = strategy,
                Category = ParseOptionalEnum<NotableDateCategory>(ruleElement, "category") ?? NotableDateCategory.None,
                FirstYear = ParseOptionalInt(ruleElement, "firstYear"),
                LastYear = ParseOptionalInt(ruleElement, "lastYear"),
                OccurrenceYears = ParseOptionalInt(ruleElement, "occurrenceYears"),
                CalendarType = ParseOptionalType<SysGlobal.Calendar>(ruleElement, "calendarType"),
                TerritoryCode = GetOptionalAttribute(ruleElement, "territory"),
                IsNonWorkingDay = ParseOptionalBool(ruleElement, "nonWorking"),
                DurationDays = ParseOptionalInt(ruleElement, "durationDays") ?? 1,
                Priority = ParseOptionalInt(ruleElement, "priority") ?? 100,
                Tags = ruleElement.Elements(s_namespace + "Tag")
                    .Select(t => t.Value)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase),
                Comment = GetOptionalAttribute(ruleElement, "comment"),
                Adjustments = adjustments,
            };

            yield return ApplyStrategySpecifics(rule, strategyElement);
        }
    }

    /// <summary>
    /// Returns <see langword="true" /> if <paramref name="localName" /> is a recognized
    /// calculation-strategy element name (for example <c>Fixed</c>, <c>EasterSunday</c>).
    /// </summary>
    /// <param name="localName">The local name of the XML element.</param>
    /// <returns><see langword="true" /> if the element names a strategy; otherwise <see langword="false" />.</returns>
    private static bool IsStrategyElement(string localName) =>
        localName is "Fixed" or "DayOfWeekInMonth" or "Algorithm" or "OffsetFromAnchor";

    /// <summary>
    /// Applies strategy-specific attributes and child elements (fixed date, Easter offset,
    /// lunar rule, and so on) onto <paramref name="rule" />, returning the enriched rule.
    /// </summary>
    /// <param name="rule">The partially-populated rule to enrich.</param>
    /// <param name="strategyElement">The XML element describing the strategy.</param>
    /// <returns>The rule with strategy-specific fields populated.</returns>
    private static NotableDateRule ApplyStrategySpecifics(NotableDateRule rule, XElement strategyElement)
    {
        if (rule.Strategy == DateResolutionStrategy.Fixed)
        {
            (var monthNum, var monthAlias) = ParseMonthToken(GetRequiredAttribute(strategyElement, "month"));
            return rule with
            {
                Month = monthNum,
                CalendarMonthAlias = monthAlias,
                Day = int.Parse(GetRequiredAttribute(strategyElement, "day"), CultureInfo.InvariantCulture),
                SkipLeapMonth = ParseOptionalBool(strategyElement, "skipLeapMonth") ?? false,
                SweepCalendarYears = ParseOptionalBool(strategyElement, "sweepCalendarYears") ?? false,
            };
        }

        return rule.Strategy switch
        {
            DateResolutionStrategy.DayOfWeekInMonth => rule with
            {
                Month = ParseMonth(GetRequiredAttribute(strategyElement, "month")),
                DayOfWeek = ParseRequiredEnum<DayOfWeek>(strategyElement, "dayOfWeek"),
                WeekOrdinal = ParseRequiredEnum<WeekOfMonthOrdinal>(strategyElement, "weekOrdinal"),
            },
            DateResolutionStrategy.OffsetFromAnchor => rule with
            {
                AnchorRuleName = GetRequiredAttribute(strategyElement, "name"),
                OffsetDays = int.Parse(GetRequiredAttribute(strategyElement, "offset"), CultureInfo.InvariantCulture),
            },
            DateResolutionStrategy.Algorithm => rule with
            {
                AlgorithmKey = GetOptionalAttribute(strategyElement, "key"),
                AlgorithmType = ParseOptionalType<INotableDateAlgorithm>(strategyElement, "type"),
                AlgorithmMonth = GetOptionalAttribute(strategyElement, "month"),
                AlgorithmDay = ParseOptionalInt(strategyElement, "day"),
            },
            _ => throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.NotSupportedException_UnsupportedStrategy, rule.Strategy)),
        };
    }

    /// <summary>
    /// Parses an &lt;Adjustment&gt; XML element into an <see cref="ObservanceAdjustment" /> record.
    /// </summary>
    /// <param name="element">The &lt;Adjustment&gt; XML element.</param>
    /// <returns>The parsed observance adjustment.</returns>
    private static ObservanceAdjustment ParseAdjustment(XElement element) =>
        new()
        {
            Key = GetRequiredAttribute(element, "key"),
            Trigger = ParseRequiredEnum<AdjustmentTrigger>(element, "when"),
            Action = ParseRequiredEnum<AdjustmentAction>(element, "action"),
            DayOfWeek = ParseOptionalEnum<DayOfWeek>(element, "dayOfWeek"),
            WeekOrdinal = ParseOptionalEnum<WeekOfMonthOrdinal>(element, "weekOrdinal"),
            IsNonWorkingDay = ParseOptionalBool(element, "nonWorking"),
            OffsetDays = ParseOptionalInt(element, "days") ?? 0,
            TerritoryCode = GetOptionalAttribute(element, "territory"),
            CalendarType = ParseOptionalType<SysGlobal.Calendar>(element, "calendarType"),
            EffectiveFromYear = ParseOptionalInt(element, "fromYear"),
            EffectiveToYear = ParseOptionalInt(element, "toYear"),
            ComparisonDate = ParseOptionalMonthDay(element, "comparisonMonth", "comparisonDay"),
            TargetRuleName = GetOptionalAttribute(element, "target"),
            Priority = ParseOptionalInt(element, "priority") ?? 100,
            HandlerKey = GetOptionalAttribute(element, "handlerKey"),
        };

    // ----------------------------------------------------------------------------
    // Attribute helpers
    // ----------------------------------------------------------------------------

    /// <summary>
    /// Returns the value of <paramref name="attributeName" /> on <paramref name="element" />,
    /// throwing if absent.
    /// </summary>
    /// <param name="element">The XML element to inspect.</param>
    /// <param name="attributeName">The required attribute name.</param>
    /// <returns>The attribute value.</returns>
    /// <exception cref="FormatException">The attribute is missing on <paramref name="element" />.</exception>
    private static string GetRequiredAttribute(XElement element, string attributeName) =>
        element.Attribute(attributeName)?.Value
            ?? throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_MissingRequiredAttribute, attributeName, element.Name.LocalName));

    /// <summary>
    /// Returns the value of <paramref name="attributeName" /> on <paramref name="element" />,
    /// or <see langword="null" /> if the attribute is absent.
    /// </summary>
    /// <param name="element">The XML element to inspect.</param>
    /// <param name="attributeName">The attribute name.</param>
    /// <returns>The attribute value, or <see langword="null" /> if not present.</returns>
    private static string? GetOptionalAttribute(XElement element, string attributeName) =>
        element.Attribute(attributeName)?.Value;

    private static TEnum ParseRequiredEnum<TEnum>(XElement element, string attributeName)
        where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(GetRequiredAttribute(element, attributeName), ignoreCase: true, out TEnum result)
            ? result
            : throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_InvalidAttributeValue, attributeName, element.Name.LocalName));

    private static TEnum? ParseOptionalEnum<TEnum>(XElement element, string attributeName)
        where TEnum : struct, Enum
    {
        var raw = GetOptionalAttribute(element, attributeName);
        return raw is not null && Enum.TryParse<TEnum>(raw, ignoreCase: true, out TEnum result) ? result : null;
    }

    /// <summary>
    /// Parses <paramref name="attributeName" /> on <paramref name="element" /> as an
    /// <see cref="int" /> if present.
    /// </summary>
    /// <param name="element">The XML element to inspect.</param>
    /// <param name="attributeName">The attribute name.</param>
    /// <returns>The parsed integer, or <see langword="null" /> if the attribute is absent.</returns>
    /// <exception cref="FormatException">The attribute is present but not a valid integer.</exception>
    private static int? ParseOptionalInt(XElement element, string attributeName)
    {
        var raw = GetOptionalAttribute(element, attributeName);
        return raw is not null && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) ? result : (int?)null;
    }

    /// <summary>
    /// Parses <paramref name="attributeName" /> on <paramref name="element" /> as a
    /// <see cref="bool" /> if present.
    /// </summary>
    /// <param name="element">The XML element to inspect.</param>
    /// <param name="attributeName">The attribute name.</param>
    /// <returns>The parsed boolean, or <see langword="null" /> if the attribute is absent.</returns>
    /// <exception cref="FormatException">The attribute is present but not a valid boolean.</exception>
    private static bool? ParseOptionalBool(XElement element, string attributeName)
    {
        var raw = GetOptionalAttribute(element, attributeName);
        return raw is not null && bool.TryParse(raw, out var result) ? result : (bool?)null;
    }

    /// <summary>
    /// Parses a (month, day) pair from <paramref name="element" /> into a
    /// <see cref="DateTime" /> in the current year, or returns <see langword="null" /> if either
    /// attribute is absent.
    /// </summary>
    /// <param name="element">The XML element to inspect.</param>
    /// <param name="monthAttr">The attribute name carrying the month name or number.</param>
    /// <param name="dayAttr">The attribute name carrying the day of month.</param>
    /// <returns>The parsed month/day, or <see langword="null" />.</returns>
    /// <exception cref="FormatException">One of the attributes is present but cannot be parsed.</exception>
    private static DateTime? ParseOptionalMonthDay(XElement element, string monthAttr, string dayAttr)
    {
        var month = GetOptionalAttribute(element, monthAttr);
        var day = GetOptionalAttribute(element, dayAttr);
        if (month is null || day is null) return null;

        var monthValue = ParseMonth(month);
        if (!int.TryParse(day, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dayValue)) return null;

        // Year is irrelevant for comparison-date authoring; the adjuster reprojects onto the resolved year.
        return new DateTime(2000, monthValue, dayValue, 0, 0, 0, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Resolves the value of <paramref name="attributeName" /> on <paramref name="element" />
    /// to a <see cref="Type" /> assignable to <typeparamref name="TBase" />.
    /// </summary>
    /// <typeparam name="TBase">The base type the resolved type must be assignable to.</typeparam>
    /// <param name="element">The XML element to inspect.</param>
    /// <param name="attributeName">The attribute name carrying the type name.</param>
    /// <returns>The resolved <see cref="Type" />, or <see langword="null" /> if the attribute is absent.</returns>
    /// <exception cref="FormatException">The attribute is present but does not resolve to a type
    /// assignable to <typeparamref name="TBase" />.</exception>
    private static Type? ParseOptionalType<TBase>(XElement element, string attributeName)
    {
        var typeName = GetOptionalAttribute(element, attributeName);
        if (string.IsNullOrWhiteSpace(typeName)) return null;

        var type = Type.GetType(typeName, throwOnError: false);
        return type is not null && typeof(TBase).IsAssignableFrom(type) ? type : null;
    }

    /// <summary>
    /// Parses <paramref name="monthName" /> as a culture-invariant English month name (e.g. <c>January</c>) or
    /// an integer month number in the range <c>1</c>–<c>13</c>.
    /// </summary>
    /// <remarks>
    /// Integer months are accepted to support non-Gregorian BCL calendar rules (e.g. <c>month="1"</c> on a
    /// <c>Fixed</c> rule with <c>calendarType="System.Globalization.ChineseLunisolarCalendar"</c>).
    /// Lunisolar calendars can carry up to 13 months in intercalary years.
    /// </remarks>
    /// <param name="monthName">The month token — either an English month name or an integer 1–13.</param>
    /// <returns>The month number.</returns>
    /// <exception cref="FormatException"><paramref name="monthName" /> is neither a recognized English month name nor an integer in 1–13.</exception>
    private static int ParseMonth(string monthName)
    {
        ThrowHelper.ThrowIfNullOrEmpty(monthName);

        if (DateTime.TryParseExact(monthName, "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            return result.Month;

        if (int.TryParse(monthName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric)
            && numeric is >= 1 and <= 13)
            return numeric;

        throw new FormatException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.FormatException_InvalidMonthValueGregorian, monthName));
    }

    /// <summary>
    /// Parses <paramref name="token" /> as a month identifier for a <c>Fixed</c> strategy element, returning
    /// either an integer month number or a calendar-specific alias string for months whose calendar position
    /// depends on whether the year is a leap year.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Accepted forms:
    /// <list type="bullet">
    ///   <item><description>Full English month name — <c>"January"</c> through <c>"December"</c>.</description></item>
    ///   <item><description>Integer 1–13 — a direct calendar month number.</description></item>
    ///   <item><description>Hebrew month name with a fixed calendar position — <c>Tishri</c> (1), <c>Heshvan</c> (2), <c>Kislev</c> (3), <c>Tevet</c> (4), <c>Shevat</c> (5), <c>AdarI</c> (6), <c>AdarII</c> (7); these are stored as integers.</description></item>
    ///   <item><description>Hebrew month name with a leap-year-dependent position — <c>LastAdar</c>, <c>Nisan</c>, <c>Iyar</c>, <c>Sivan</c>, <c>Tammuz</c>, <c>Av</c>, <c>Elul</c>; these are stored as alias strings and resolved by the resolver at runtime.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="token">The month token from the XML attribute.</param>
    /// <returns>
    /// A tuple of <c>(numericMonth, alias)</c>: exactly one of the two is non-<see langword="null" />.
    /// </returns>
    /// <exception cref="FormatException"><paramref name="token" /> is not a recognized month token.</exception>
    private static (int? numericMonth, string? alias) ParseMonthToken(string token)
    {
        ThrowHelper.ThrowIfNullOrEmpty(token);

        if (DateTime.TryParseExact(token, "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            return (result.Month, null);

        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric)
            && numeric is >= 1 and <= 13)
            return (numeric, null);

        // Hebrew months with a fixed calendar position — always at the same 1-based index.
        var simpleHebrew = token switch
        {
            "Tishri" => 1,
            "Heshvan" => 2,
            "Kislev" => 3,
            "Tevet" => 4,
            "Shevat" => 5,
            "AdarI" => 6,
            "AdarII" => 7,
            _ => (int?)null,
        };

        if (simpleHebrew is not null)
            return (simpleHebrew, null);

        // Hebrew months whose calendar position shifts in leap years — stored as a named alias
        // for runtime resolution by the resolver.
        if (token is "LastAdar" or "Nisan" or "Iyar" or "Sivan" or "Tammuz" or "Av" or "Elul")
            return (null, token);

        throw new FormatException(
            string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.FormatException_InvalidMonthValueHebrew, token));
    }

    // ----------------------------------------------------------------------------
    // Schema validation
    // ----------------------------------------------------------------------------

    /// <summary>
    /// Loads the embedded XSD schema set used to validate notable-date XML documents.
    /// </summary>
    /// <returns>The compiled <see cref="XmlSchemaSet" />.</returns>
    private static XmlSchemaSet LoadSchema()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string schemaResourceName = "Bodu.Globalization.Calendar.NotableDates.xsd";

        using Stream stream = assembly.GetManifestResourceStream(schemaResourceName)
            ?? throw new FileNotFoundException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.FileNotFoundException_EmbeddedSchemaResourceNotFound, schemaResourceName, assembly.FullName));

        var schemaSet = new XmlSchemaSet();
        schemaSet.Add(null, XmlReader.Create(stream));
        return schemaSet;
    }

    /// <summary>
    /// Builds the <see cref="XmlReaderSettings" /> used to validate notable-date XML documents
    /// against the embedded schema.
    /// </summary>
    /// <returns>Configured reader settings with schema validation enabled.</returns>
    private static XmlReaderSettings CreateValidationSettings()
    {
        var settings = new XmlReaderSettings
        {
            ValidationType = ValidationType.Schema,
            Schemas = s_schemaSet,
            ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings,
        };
        settings.ValidationEventHandler += HandleValidationEvent;
        return settings;
    }

    /// <summary>
    /// Validates <paramref name="document" /> against the embedded notable-date schema,
    /// throwing on the first schema violation.
    /// </summary>
    /// <param name="document">The XML document to validate.</param>
    /// <exception cref="XmlSchemaValidationException">The document fails schema validation.</exception>
    private static void ValidateDocument(XDocument document)
    {
        using XmlReader reader = document.CreateReader();
        using var validatingReader = XmlReader.Create(reader, CreateValidationSettings());
        while (validatingReader.Read()) { }
    }

    /// <summary>
    /// Schema-validation event handler that rethrows warnings and errors as
    /// <see cref="XmlSchemaValidationException" />.
    /// </summary>
    /// <param name="sender">The event sender (unused).</param>
    /// <param name="e">The validation event arguments.</param>
    /// <exception cref="XmlSchemaValidationException">Always thrown for any reported event.</exception>
    private static void HandleValidationEvent(object? sender, ValidationEventArgs e)
    {
        if (e.Severity == XmlSeverityType.Error)
            throw new XmlSchemaValidationException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.XmlSchemaValidationException_SchemaValidationError, e.Message), e.Exception);
    }
}
