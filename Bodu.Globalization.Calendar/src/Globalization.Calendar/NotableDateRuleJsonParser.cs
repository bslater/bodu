// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleJsonParser.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using Json.Schema;
using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Parses authored JSON payloads into <see cref="NotableDateRule" /> instances after schema validation.
/// Companion to <see cref="NotableDateRuleParser" /> for authors who prefer a JSON authoring format.
/// </summary>
/// <remarks>
/// <para>
/// JSON inputs are validated against the embedded <c>NotableDates.schema.json</c> (a JSON Schema
/// draft 2020-12 document that mirrors <c>NotableDates.xsd</c>) before parsing. Schema violations
/// surface as <see cref="JsonException" /> so authoring errors are caught at parse time rather than
/// at first query — exactly as the XML pathway surfaces <see cref="System.Xml.Schema.XmlSchemaValidationException" />.
/// </para>
/// <para>
/// The JSON vocabulary mirrors the XML schema one-for-one: a top-level object with optional
/// <c>useFrom</c> and <c>notableDates</c> arrays; each rule selects exactly one of <c>fixed</c>,
/// <c>dayOfWeekInMonth</c>, <c>offsetFromAnchor</c>, or <c>algorithm</c> as its strategy. After
/// schema validation succeeds, a semantic pass enforces the remaining invariants the XML parser
/// enforces — unique adjustment keys within a rule, resolvable enum/type names, and the override-body
/// name/category requirement.
/// </para>
/// </remarks>
public static class NotableDateRuleJsonParser
{
    private static readonly JsonSerializerOptions s_serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = false,
    };

    private static readonly JsonSchema s_schema = LoadSchema();

    private static readonly EvaluationOptions s_validationOptions = new()
    {
        OutputFormat = OutputFormat.List,
    };

    /// <summary>
    /// Parses the supplied JSON string into rules after validating it against the embedded schema.
    /// </summary>
    /// <param name="json">The JSON payload. Must not be <see langword="null" />, empty, or whitespace.</param>
    /// <returns>The parsed rules.</returns>
    /// <remarks>
    /// This convenience overload returns only the rules and discards any <c>useFrom</c> directives. To
    /// resolve a document graph including imports, call <see cref="ParseDocument(string)" /> instead and
    /// feed the result to a loader such as <see cref="JsonResourceNotableDateRuleProvider" />.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json" /> is <see langword="null" />, empty, or whitespace.</exception>
    /// <exception cref="JsonException">Thrown when <paramref name="json" /> is not well-formed JSON, or fails schema validation against the embedded <c>NotableDates.schema.json</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the document fails semantic validation.</exception>
    public static List<NotableDateRule> ParseJson(string json) =>
        [.. ParseDocument(json).LocalRules];

    /// <summary>
    /// Parses the supplied JSON string into a <see cref="ParsedNotableDateDocument" />, exposing local rules
    /// together with any <c>useFrom</c> directives, after validating it against the embedded schema.
    /// </summary>
    /// <param name="json">The JSON payload. Must not be <see langword="null" />, empty, or whitespace.</param>
    /// <returns>The parsed document, including imports and rules.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json" /> is <see langword="null" />, empty, or whitespace.</exception>
    /// <exception cref="JsonException">Thrown when <paramref name="json" /> is not well-formed JSON, or fails schema validation against the embedded <c>NotableDates.schema.json</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the document fails semantic validation.</exception>
    public static ParsedNotableDateDocument ParseDocument(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentNullException(nameof(json));

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = false,
            });
        }
        catch (JsonException ex)
        {
            // JsonNode.Parse can leak the internal JsonReaderException subclass; re-throw as
            // the documented public type so the parser surfaces a single, stable exception type
            // — matching how XmlReader surfaces XmlException directly for malformed XML.
            throw new JsonException(ex.Message, ex.Path, ex.LineNumber, ex.BytePositionInLine);
        }

        ValidateDocument(node);

        // Schema validation enforces type:object at the root, so a successful Validate guarantees a non-null tree.
        NotableDatesDocumentDto dto = node!.Deserialize<NotableDatesDocumentDto>(s_serializerOptions)
            ?? throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_MissingRequiredAttribute, "root", "NotableDates"));

        var useGroups = (dto.UseFrom ?? [])
            .Select(MapUseGroup)
            .ToImmutableArray();

        var localRules = (dto.NotableDates ?? [])
            .SelectMany(MapNotableDate)
            .ToImmutableArray();

        return new ParsedNotableDateDocument(useGroups, localRules);
    }

    // ----------------------------------------------------------------------------
    // DTO → record mapping
    // ----------------------------------------------------------------------------

    /// <summary>
    /// Maps a <see cref="UseFromDto" /> onto a <see cref="NotableDateRuleUseGroup" />.
    /// </summary>
    /// <param name="dto">The DTO to map. Must declare a non-empty resource path.</param>
    /// <returns>The mapped <see cref="NotableDateRuleUseGroup" />.</returns>
    private static NotableDateRuleUseGroup MapUseGroup(UseFromDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Resource))
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_MissingRequiredAttribute, "resource", "useFrom"));

        var uses = (dto.Uses ?? [])
            .Select(MapUseDirective)
            .ToImmutableArray();

        return new NotableDateRuleUseGroup(dto.Resource, dto.UseAll ?? false, uses);
    }

    /// <summary>
    /// Maps a <see cref="UseDto" /> onto a <see cref="NotableDateRuleUseDirective" />.
    /// </summary>
    /// <param name="dto">The DTO to map. Must declare a non-empty source rule name.</param>
    /// <returns>The mapped <see cref="NotableDateRuleUseDirective" />.</returns>
    private static NotableDateRuleUseDirective MapUseDirective(UseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_MissingRequiredAttribute, "name", "use"));

        NotableDateRuleOverrideBody? overrideBody = dto.Rule is null ? null : MapOverrideBody(dto.Rule);

        return new NotableDateRuleUseDirective(
            SourceRuleName: dto.Name,
            LocalName: dto.As,
            Category: ParseOptionalEnum<NotableDateCategory>(dto.Category, "category", "use"),
            TerritoryCode: dto.Territory,
            IsNonWorkingDay: dto.NonWorking,
            FirstYear: dto.FirstYear,
            LastYear: dto.LastYear,
            OccurrenceYears: dto.OccurrenceYears,
            DurationDays: dto.DurationDays,
            Priority: dto.Priority,
            Comment: dto.Comment,
            ClearTags: dto.ClearTags ?? false,
            ClearAdjustments: dto.ClearAdjustments ?? false,
            ClearInherited: dto.ClearInherited ?? false,
            OverrideBody: overrideBody);
    }

    /// <summary>
    /// Maps an <see cref="OverrideRuleDto" /> onto a <see cref="NotableDateRuleOverrideBody" />, enforcing
    /// the XSD-mandated <c>name</c> and <c>category</c> requirement on the override body.
    /// </summary>
    /// <param name="dto">The override DTO to map.</param>
    /// <returns>The mapped <see cref="NotableDateRuleOverrideBody" />.</returns>
    private static NotableDateRuleOverrideBody MapOverrideBody(OverrideRuleDto dto)
    {
        var ruleName = RequireString(dto.Name, "name", "rule");
        NotableDateCategory category = ParseRequiredEnum<NotableDateCategory>(dto.Category, "category", "rule");
        DateResolutionStrategy? strategy = DetectOverrideStrategy(dto);

        var body = new NotableDateRuleOverrideBody
        {
            RuleName = ruleName,
            Category = category,
            TerritoryCode = dto.Territory,
            IsNonWorkingDay = dto.NonWorking,
            FirstYear = dto.FirstYear,
            LastYear = dto.LastYear,
            OccurrenceYears = dto.OccurrenceYears,
            DurationDays = dto.DurationDays,
            Priority = dto.Priority,
            Comment = dto.Comment,
            CalendarType = ParseOptionalType<SysGlobal.Calendar>(dto.CalendarType),
            Strategy = strategy,
            Tags = [.. (dto.Tags ?? []).Where(t => !string.IsNullOrWhiteSpace(t))],
            Adjustments = [.. (dto.Adjustments ?? []).Select(MapAdjustment)],
        };

        if (strategy is not null)
            body = ApplyStrategySpecificsToBody(body, dto);

        EnsureUniqueAdjustmentKeys(body.Adjustments, "rule");
        return body;
    }

    /// <summary>
    /// Expands a <see cref="NotableDateDto" /> into one rule per <c>rules[]</c> entry.
    /// </summary>
    /// <param name="dto">The notable-date DTO whose rules are being expanded. Must declare a non-empty name.</param>
    /// <returns>The sequence of rules derived from the DTO.</returns>
    private static IEnumerable<NotableDateRule> MapNotableDate(NotableDateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_MissingRequiredAttribute, "name", "notableDate"));

        foreach (RuleDto ruleDto in dto.Rules ?? [])
        {
            yield return MapRule(dto.Name, ruleDto);
        }
    }

    /// <summary>
    /// Maps a single <see cref="RuleDto" /> onto a fully populated <see cref="NotableDateRule" />.
    /// </summary>
    /// <param name="notableDateName">The owning notable-date name.</param>
    /// <param name="dto">The rule DTO to map.</param>
    /// <returns>The mapped <see cref="NotableDateRule" />.</returns>
    private static NotableDateRule MapRule(string notableDateName, RuleDto dto)
    {
        DateResolutionStrategy strategy = DetectRuleStrategy(dto, notableDateName);

        var adjustments = (dto.Adjustments ?? [])
            .Select(MapAdjustment)
            .ToImmutableArray();

        EnsureUniqueAdjustmentKeys(adjustments, "rule");

        var rule = new NotableDateRule
        {
            Name = notableDateName,
            RuleName = dto.Name,
            Strategy = strategy,
            Category = ParseOptionalEnum<NotableDateCategory>(dto.Category, "category", "rule") ?? NotableDateCategory.None,
            FirstYear = dto.FirstYear,
            LastYear = dto.LastYear,
            OccurrenceYears = dto.OccurrenceYears,
            CalendarType = ParseOptionalType<SysGlobal.Calendar>(dto.CalendarType),
            TerritoryCode = dto.Territory,
            IsNonWorkingDay = dto.NonWorking,
            DurationDays = dto.DurationDays ?? 1,
            Priority = dto.Priority ?? 100,
            Tags = (dto.Tags ?? [])
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase),
            Comment = dto.Comment,
            Adjustments = adjustments,
        };

        return ApplyStrategySpecifics(rule, dto);
    }

    /// <summary>
    /// Maps an <see cref="AdjustmentDto" /> onto an <see cref="ObservanceAdjustment" />.
    /// </summary>
    /// <param name="dto">The adjustment DTO to map. Must declare a non-empty key.</param>
    /// <returns>The mapped <see cref="ObservanceAdjustment" />.</returns>
    private static ObservanceAdjustment MapAdjustment(AdjustmentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Key))
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_MissingRequiredAttribute, "key", "adjustment"));

        return new ObservanceAdjustment
        {
            Key = dto.Key,
            Trigger = ParseRequiredEnum<AdjustmentTrigger>(dto.When, "when", "adjustment"),
            Action = ParseRequiredEnum<AdjustmentAction>(dto.Action, "action", "adjustment"),
            DayOfWeek = ParseOptionalEnum<DayOfWeek>(dto.DayOfWeek, "dayOfWeek", "adjustment"),
            WeekOrdinal = ParseOptionalEnum<WeekOfMonthOrdinal>(dto.WeekOrdinal, "weekOrdinal", "adjustment"),
            IsNonWorkingDay = dto.NonWorking,
            OffsetDays = dto.Days ?? 0,
            TerritoryCode = dto.Territory,
            CalendarType = ParseOptionalType<SysGlobal.Calendar>(dto.CalendarType),
            EffectiveFromYear = dto.FromYear,
            EffectiveToYear = dto.ToYear,
            ComparisonDate = ParseOptionalMonthDay(dto.ComparisonMonth, dto.ComparisonDay),
            TargetRuleName = dto.Target,
            Priority = dto.Priority ?? 100,
            HandlerKey = dto.HandlerKey,
        };
    }

    // ----------------------------------------------------------------------------
    // Strategy detection and projection
    // ----------------------------------------------------------------------------

    /// <summary>
    /// Determines which strategy a <see cref="RuleDto" /> declared, enforcing exactly-one-of-N selection.
    /// </summary>
    /// <param name="dto">The rule DTO whose strategy is being inspected.</param>
    /// <param name="notableDateName">The owning notable-date name, used for diagnostic messages.</param>
    /// <returns>The detected <see cref="DateResolutionStrategy" />.</returns>
    private static DateResolutionStrategy DetectRuleStrategy(RuleDto dto, string notableDateName)
    {
        var count = 0;
        DateResolutionStrategy result = default;

        if (dto.Fixed is not null) { result = DateResolutionStrategy.Fixed; count++; }
        if (dto.DayOfWeekInMonth is not null) { result = DateResolutionStrategy.DayOfWeekInMonth; count++; }
        if (dto.OffsetFromAnchor is not null) { result = DateResolutionStrategy.OffsetFromAnchor; count++; }
        if (dto.Algorithm is not null) { result = DateResolutionStrategy.Algorithm; count++; }

        if (count == 0)
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_RuleMissingStrategy, notableDateName));

        if (count > 1)
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_UnknownStrategyElementOnRule, "<multiple>", notableDateName));

        return result;
    }

    /// <summary>
    /// Determines which strategy an <see cref="OverrideRuleDto" /> declared, allowing zero or one. Returns
    /// <see langword="null" /> when no strategy is specified (the override inherits the source's strategy).
    /// </summary>
    /// <param name="dto">The override DTO whose strategy is being inspected.</param>
    /// <returns>The detected strategy, or <see langword="null" /> when none is declared.</returns>
    private static DateResolutionStrategy? DetectOverrideStrategy(OverrideRuleDto dto)
    {
        var count = 0;
        DateResolutionStrategy result = default;

        if (dto.Fixed is not null) { result = DateResolutionStrategy.Fixed; count++; }
        if (dto.DayOfWeekInMonth is not null) { result = DateResolutionStrategy.DayOfWeekInMonth; count++; }
        if (dto.OffsetFromAnchor is not null) { result = DateResolutionStrategy.OffsetFromAnchor; count++; }
        if (dto.Algorithm is not null) { result = DateResolutionStrategy.Algorithm; count++; }

        if (count == 0)
            return null;

        if (count > 1)
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_UnknownStrategyElementOnOverrideRule, "<multiple>"));

        return result;
    }

    /// <summary>
    /// Applies strategy-specific fields from <paramref name="dto" /> onto <paramref name="rule" />.
    /// </summary>
    /// <param name="rule">The rule receiving strategy-specific fields.</param>
    /// <param name="dto">The rule DTO carrying the strategy specifics.</param>
    /// <returns>The rule with strategy-specific fields populated.</returns>
    private static NotableDateRule ApplyStrategySpecifics(NotableDateRule rule, RuleDto dto)
    {
        switch (rule.Strategy)
        {
            case DateResolutionStrategy.Fixed:
                {
                    FixedDto f = dto.Fixed!;
                    (var monthNum, var monthAlias) = ParseMonthToken(RequireString(f.Month, "month", "fixed"));
                    return rule with
                    {
                        Month = monthNum,
                        CalendarMonthAlias = monthAlias,
                        Day = RequireInt(f.Day, "day", "fixed"),
                        SkipLeapMonth = f.SkipLeapMonth ?? false,
                        SweepCalendarYears = f.SweepCalendarYears ?? false,
                    };
                }

            case DateResolutionStrategy.DayOfWeekInMonth:
                {
                    DayOfWeekInMonthDto d = dto.DayOfWeekInMonth!;
                    return rule with
                    {
                        Month = ParseMonth(RequireString(d.Month, "month", "dayOfWeekInMonth")),
                        DayOfWeek = ParseRequiredEnum<DayOfWeek>(d.DayOfWeek, "dayOfWeek", "dayOfWeekInMonth"),
                        WeekOrdinal = ParseRequiredEnum<WeekOfMonthOrdinal>(d.WeekOrdinal, "weekOrdinal", "dayOfWeekInMonth"),
                    };
                }

            case DateResolutionStrategy.OffsetFromAnchor:
                {
                    OffsetFromAnchorDto o = dto.OffsetFromAnchor!;
                    return rule with
                    {
                        AnchorRuleName = RequireString(o.Name, "name", "offsetFromAnchor"),
                        OffsetDays = RequireInt(o.Offset, "offset", "offsetFromAnchor"),
                    };
                }

            case DateResolutionStrategy.Algorithm:
                {
                    AlgorithmDto a = dto.Algorithm!;
                    return rule with
                    {
                        AlgorithmKey = a.Key,
                        AlgorithmType = ParseOptionalType<INotableDateAlgorithm>(a.Type),
                        AlgorithmMonth = a.Month,
                        AlgorithmDay = a.Day,
                    };
                }

            default:
                throw new NotSupportedException(
                    string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.NotSupportedException_UnsupportedStrategy, rule.Strategy));
        }
    }

    /// <summary>
    /// Applies strategy-specific fields from an override DTO onto <paramref name="body" />.
    /// </summary>
    /// <param name="body">The override body receiving strategy-specific fields.</param>
    /// <param name="dto">The override DTO carrying the strategy specifics.</param>
    /// <returns>The override body with strategy-specific fields populated.</returns>
    private static NotableDateRuleOverrideBody ApplyStrategySpecificsToBody(NotableDateRuleOverrideBody body, OverrideRuleDto dto)
    {
        switch (body.Strategy)
        {
            case DateResolutionStrategy.Fixed:
                {
                    FixedDto f = dto.Fixed!;
                    (var monthNum, var monthAlias) = ParseMonthToken(RequireString(f.Month, "month", "fixed"));
                    return body with
                    {
                        Month = monthNum,
                        CalendarMonthAlias = monthAlias,
                        Day = RequireInt(f.Day, "day", "fixed"),
                        SkipLeapMonth = f.SkipLeapMonth ?? false,
                        SweepCalendarYears = f.SweepCalendarYears ?? false,
                    };
                }

            case DateResolutionStrategy.DayOfWeekInMonth:
                {
                    DayOfWeekInMonthDto d = dto.DayOfWeekInMonth!;
                    return body with
                    {
                        Month = ParseMonth(RequireString(d.Month, "month", "dayOfWeekInMonth")),
                        DayOfWeek = ParseRequiredEnum<DayOfWeek>(d.DayOfWeek, "dayOfWeek", "dayOfWeekInMonth"),
                        WeekOrdinal = ParseRequiredEnum<WeekOfMonthOrdinal>(d.WeekOrdinal, "weekOrdinal", "dayOfWeekInMonth"),
                    };
                }

            case DateResolutionStrategy.OffsetFromAnchor:
                {
                    OffsetFromAnchorDto o = dto.OffsetFromAnchor!;
                    return body with
                    {
                        AnchorRuleName = RequireString(o.Name, "name", "offsetFromAnchor"),
                        OffsetDays = RequireInt(o.Offset, "offset", "offsetFromAnchor"),
                    };
                }

            case DateResolutionStrategy.Algorithm:
                {
                    AlgorithmDto a = dto.Algorithm!;
                    return body with
                    {
                        AlgorithmKey = a.Key,
                        AlgorithmType = ParseOptionalType<INotableDateAlgorithm>(a.Type),
                        AlgorithmMonth = a.Month,
                        AlgorithmDay = a.Day,
                    };
                }

            default:
                return body;
        }
    }

    // ----------------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------------

    /// <summary>
    /// Returns <paramref name="value" /> if non-empty, otherwise throws referring to the field by name.
    /// </summary>
    /// <param name="value">The candidate value to validate.</param>
    /// <param name="fieldName">The field name used in diagnostic messages.</param>
    /// <param name="contextName">The owning context name used in diagnostic messages.</param>
    /// <returns>The validated non-empty string.</returns>
    private static string RequireString(string? value, string fieldName, string contextName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_MissingRequiredAttribute, fieldName, contextName));

        return value;
    }

    /// <summary>
    /// Returns <paramref name="value" /> if it has a value, otherwise throws referring to the field by name.
    /// </summary>
    /// <param name="value">The candidate value to validate.</param>
    /// <param name="fieldName">The field name used in diagnostic messages.</param>
    /// <param name="contextName">The owning context name used in diagnostic messages.</param>
    /// <returns>The validated integer value.</returns>
    private static int RequireInt(int? value, string fieldName, string contextName)
    {
        if (value is null)
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_MissingRequiredAttribute, fieldName, contextName));

        return value.Value;
    }

    /// <summary>
    /// Parses a required enum-valued field, throwing when missing or unrecognised.
    /// </summary>
    /// <typeparam name="TEnum">The target enum type.</typeparam>
    /// <param name="raw">The raw string value to parse.</param>
    /// <param name="fieldName">The field name used in diagnostic messages.</param>
    /// <param name="contextName">The owning context name used in diagnostic messages.</param>
    /// <returns>The parsed enum value.</returns>
    private static TEnum ParseRequiredEnum<TEnum>(string? raw, string fieldName, string contextName)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_MissingRequiredAttribute, fieldName, contextName));

        if (!Enum.TryParse<TEnum>(raw, ignoreCase: true, out TEnum result))
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_InvalidAttributeValue, fieldName, contextName));

        return result;
    }

    /// <summary>
    /// Parses an optional enum-valued field, returning <see langword="null" /> when absent.
    /// </summary>
    /// <typeparam name="TEnum">The target enum type.</typeparam>
    /// <param name="raw">The raw string value to parse, or <see langword="null" />.</param>
    /// <param name="fieldName">The field name used in diagnostic messages.</param>
    /// <param name="contextName">The owning context name used in diagnostic messages.</param>
    /// <returns>The parsed enum value, or <see langword="null" /> when <paramref name="raw" /> is absent.</returns>
    private static TEnum? ParseOptionalEnum<TEnum>(string? raw, string fieldName, string contextName)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        if (!Enum.TryParse<TEnum>(raw, ignoreCase: true, out TEnum result))
            throw new InvalidOperationException(
                string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_InvalidAttributeValue, fieldName, contextName));

        return result;
    }

    /// <summary>
    /// Resolves <paramref name="typeName" /> to a CLR type assignable to <typeparamref name="TBase" />.
    /// </summary>
    /// <typeparam name="TBase">The base type the resolved type must be assignable to.</typeparam>
    /// <param name="typeName">The fully qualified type name to resolve, or <see langword="null" />.</param>
    /// <returns>The resolved <see cref="Type" />, or <see langword="null" /> when the name is absent or unresolvable.</returns>
    private static Type? ParseOptionalType<TBase>(string? typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            return null;

        var type = Type.GetType(typeName, throwOnError: false);
        return type is not null && typeof(TBase).IsAssignableFrom(type) ? type : null;
    }

    /// <summary>
    /// Builds a synthetic comparison <see cref="DateTime" /> from a (month, day) pair, returning
    /// <see langword="null" /> when either is absent.
    /// </summary>
    /// <param name="month">The month token, or <see langword="null" />.</param>
    /// <param name="day">The day of month, or <see langword="null" />.</param>
    /// <returns>The synthetic <see cref="DateTime" />, or <see langword="null" /> when either input is absent.</returns>
    private static DateTime? ParseOptionalMonthDay(string? month, int? day)
    {
        if (string.IsNullOrWhiteSpace(month) || day is null)
            return null;

        var monthValue = ParseMonth(month);
        return new DateTime(2000, monthValue, day.Value, 0, 0, 0, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Parses an English Gregorian month name or numeric month token (1–13).
    /// </summary>
    /// <param name="monthName">The month token to parse.</param>
    /// <returns>The parsed month number.</returns>
    private static int ParseMonth(string monthName)
    {
        if (string.IsNullOrWhiteSpace(monthName))
            throw new ArgumentNullException(nameof(monthName));

        if (DateTime.TryParseExact(monthName, "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            return result.Month;

        if (int.TryParse(monthName, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric)
            && numeric is >= 1 and <= 13)
            return numeric;

        throw new FormatException(
            string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.FormatException_InvalidMonthValueGregorian, monthName));
    }

    /// <summary>
    /// Parses a Fixed-strategy month token, returning either a numeric month or a Hebrew calendar alias.
    /// </summary>
    /// <param name="token">The month token to parse.</param>
    /// <returns>A tuple of <c>(numericMonth, alias)</c>: exactly one of the two is non-<see langword="null" />.</returns>
    private static (int? numericMonth, string? alias) ParseMonthToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentNullException(nameof(token));

        if (DateTime.TryParseExact(token, "MMMM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            return (result.Month, null);

        if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric)
            && numeric is >= 1 and <= 13)
            return (numeric, null);

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

        if (token is "LastAdar" or "Nisan" or "Iyar" or "Sivan" or "Tammuz" or "Av" or "Elul")
            return (null, token);

        throw new FormatException(
            string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.FormatException_InvalidMonthValueHebrew, token));
    }

    /// <summary>
    /// Enforces the per-rule uniqueness invariant on adjustment keys.
    /// </summary>
    /// <param name="adjustments">The adjustments whose keys are being validated.</param>
    /// <param name="contextName">The owning context name used in diagnostic messages.</param>
    private static void EnsureUniqueAdjustmentKeys(ImmutableArray<ObservanceAdjustment> adjustments, string contextName)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ObservanceAdjustment adjustment in adjustments)
        {
            if (!seen.Add(adjustment.Key))
                throw new InvalidOperationException(
                    string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_DuplicateAdjustmentKey, adjustment.Key, contextName));
        }
    }

    // ----------------------------------------------------------------------------
    // Schema validation
    // ----------------------------------------------------------------------------

    /// <summary>
    /// Loads the embedded JSON Schema used to validate notable-date JSON documents.
    /// </summary>
    /// <returns>The compiled <see cref="JsonSchema" />.</returns>
    private static JsonSchema LoadSchema()
    {
        var assembly = Assembly.GetExecutingAssembly();
        const string schemaResourceName = "Bodu.Globalization.Calendar.NotableDates.schema.json";

        using Stream stream = assembly.GetManifestResourceStream(schemaResourceName)
            ?? throw new FileNotFoundException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CalendarResourceStrings.FileNotFoundException_EmbeddedSchemaResourceNotFound,
                    schemaResourceName,
                    assembly.FullName));

        using var reader = new StreamReader(stream);
        return JsonSchema.FromText(reader.ReadToEnd());
    }

    /// <summary>
    /// Validates the parsed JSON tree against the embedded schema, throwing on the first
    /// schema violation.
    /// </summary>
    /// <param name="node">The parsed JSON node to validate.</param>
    /// <exception cref="JsonException">The document fails schema validation.</exception>
    private static void ValidateDocument(JsonNode? node)
    {
        EvaluationResults results = s_schema.Evaluate(node, s_validationOptions);
        if (results.IsValid) return;

        throw new JsonException(
            string.Format(
                CultureInfo.InvariantCulture,
                CalendarResourceStrings.JsonException_SchemaValidationError,
                FormatValidationFailures(results)));
    }

    /// <summary>
    /// Flattens a failed schema evaluation into a single diagnostic message that lists each
    /// failing instance location with the keyword(s) that rejected it.
    /// </summary>
    /// <param name="results">The failed evaluation results to summarise.</param>
    /// <returns>A semicolon-separated diagnostic message.</returns>
    private static string FormatValidationFailures(EvaluationResults results)
    {
        var builder = new StringBuilder();
        AppendFailures(builder, results);
        return builder.Length == 0 ? "schema validation failed" : builder.ToString();
    }

    /// <summary>
    /// Appends every failing detail in <paramref name="results" /> onto <paramref name="builder" />.
    /// </summary>
    /// <param name="builder">The buffer that accumulates the diagnostic message.</param>
    /// <param name="results">The evaluation results to walk for failures.</param>
    private static void AppendFailures(StringBuilder builder, EvaluationResults results)
    {
        if (results.Errors is { Count: > 0 })
        {
            foreach (KeyValuePair<string, string> error in results.Errors)
            {
                if (builder.Length > 0) builder.Append("; ");
                builder.Append(results.InstanceLocation);
                builder.Append(": ");
                builder.Append(error.Key);
                builder.Append(' ');
                builder.Append(error.Value);
            }
        }

        if (results.Details is { Count: > 0 })
        {
            foreach (EvaluationResults detail in results.Details)
            {
                if (!detail.IsValid)
                    AppendFailures(builder, detail);
            }
        }
    }

    // ----------------------------------------------------------------------------
    // DTOs (private — bound to the JSON wire format only)
    // ----------------------------------------------------------------------------

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1134:Attributes should not share line")]
    private sealed class NotableDatesDocumentDto
    {
        [JsonPropertyName("useFrom")] public UseFromDto[]? UseFrom { get; init; }
        [JsonPropertyName("notableDates")] public NotableDateDto[]? NotableDates { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1134:Attributes should not share line")]
    private sealed class UseFromDto
    {
        [JsonPropertyName("resource")] public string? Resource { get; init; }
        [JsonPropertyName("useAll")] public bool? UseAll { get; init; }
        [JsonPropertyName("uses")] public UseDto[]? Uses { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1134:Attributes should not share line")]
    private sealed class UseDto
    {
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("as")] public string? As { get; init; }
        [JsonPropertyName("category")] public string? Category { get; init; }
        [JsonPropertyName("territory")] public string? Territory { get; init; }
        [JsonPropertyName("nonWorking")] public bool? NonWorking { get; init; }
        [JsonPropertyName("firstYear")] public int? FirstYear { get; init; }
        [JsonPropertyName("lastYear")] public int? LastYear { get; init; }
        [JsonPropertyName("occurrenceYears")] public int? OccurrenceYears { get; init; }
        [JsonPropertyName("durationDays")] public int? DurationDays { get; init; }
        [JsonPropertyName("priority")] public int? Priority { get; init; }
        [JsonPropertyName("comment")] public string? Comment { get; init; }
        [JsonPropertyName("clearTags")] public bool? ClearTags { get; init; }
        [JsonPropertyName("clearAdjustments")] public bool? ClearAdjustments { get; init; }
        [JsonPropertyName("clearInherited")] public bool? ClearInherited { get; init; }
        [JsonPropertyName("rule")] public OverrideRuleDto? Rule { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1134:Attributes should not share line")]
    private sealed class NotableDateDto
    {
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("rules")] public RuleDto[]? Rules { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1134:Attributes should not share line")]
    private class RuleDto
    {
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("category")] public string? Category { get; init; }
        [JsonPropertyName("firstYear")] public int? FirstYear { get; init; }
        [JsonPropertyName("lastYear")] public int? LastYear { get; init; }
        [JsonPropertyName("occurrenceYears")] public int? OccurrenceYears { get; init; }
        [JsonPropertyName("durationDays")] public int? DurationDays { get; init; }
        [JsonPropertyName("priority")] public int? Priority { get; init; }
        [JsonPropertyName("nonWorking")] public bool? NonWorking { get; init; }
        [JsonPropertyName("calendarType")] public string? CalendarType { get; init; }
        [JsonPropertyName("territory")] public string? Territory { get; init; }
        [JsonPropertyName("comment")] public string? Comment { get; init; }
        [JsonPropertyName("fixed")] public FixedDto? Fixed { get; init; }
        [JsonPropertyName("dayOfWeekInMonth")] public DayOfWeekInMonthDto? DayOfWeekInMonth { get; init; }
        [JsonPropertyName("offsetFromAnchor")] public OffsetFromAnchorDto? OffsetFromAnchor { get; init; }
        [JsonPropertyName("algorithm")] public AlgorithmDto? Algorithm { get; init; }
        [JsonPropertyName("tags")] public string[]? Tags { get; init; }
        [JsonPropertyName("adjustments")] public AdjustmentDto[]? Adjustments { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1134:Attributes should not share line")]
    private sealed class OverrideRuleDto : RuleDto;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1134:Attributes should not share line")]
    private sealed class FixedDto
    {
        [JsonPropertyName("month")] public string? Month { get; init; }
        [JsonPropertyName("day")] public int? Day { get; init; }
        [JsonPropertyName("skipLeapMonth")] public bool? SkipLeapMonth { get; init; }
        [JsonPropertyName("sweepCalendarYears")] public bool? SweepCalendarYears { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1134:Attributes should not share line")]
    private sealed class DayOfWeekInMonthDto
    {
        [JsonPropertyName("month")] public string? Month { get; init; }
        [JsonPropertyName("dayOfWeek")] public string? DayOfWeek { get; init; }
        [JsonPropertyName("weekOrdinal")] public string? WeekOrdinal { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1134:Attributes should not share line")]
    private sealed class OffsetFromAnchorDto
    {
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("offset")] public int? Offset { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1134:Attributes should not share line")]
    private sealed class AlgorithmDto
    {
        [JsonPropertyName("key")] public string? Key { get; init; }
        [JsonPropertyName("type")] public string? Type { get; init; }
        [JsonPropertyName("month")] public string? Month { get; init; }
        [JsonPropertyName("day")] public int? Day { get; init; }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1134:Attributes should not share line")]
    private sealed class AdjustmentDto
    {
        [JsonPropertyName("key")] public string? Key { get; init; }
        [JsonPropertyName("when")] public string? When { get; init; }
        [JsonPropertyName("action")] public string? Action { get; init; }
        [JsonPropertyName("dayOfWeek")] public string? DayOfWeek { get; init; }
        [JsonPropertyName("weekOrdinal")] public string? WeekOrdinal { get; init; }
        [JsonPropertyName("days")] public int? Days { get; init; }
        [JsonPropertyName("priority")] public int? Priority { get; init; }
        [JsonPropertyName("nonWorking")] public bool? NonWorking { get; init; }
        [JsonPropertyName("territory")] public string? Territory { get; init; }
        [JsonPropertyName("calendarType")] public string? CalendarType { get; init; }
        [JsonPropertyName("fromYear")] public int? FromYear { get; init; }
        [JsonPropertyName("toYear")] public int? ToYear { get; init; }
        [JsonPropertyName("comparisonMonth")] public string? ComparisonMonth { get; init; }
        [JsonPropertyName("comparisonDay")] public int? ComparisonDay { get; init; }
        [JsonPropertyName("target")] public string? Target { get; init; }
        [JsonPropertyName("handlerKey")] public string? HandlerKey { get; init; }
    }
}
