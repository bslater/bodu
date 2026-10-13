// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilder.Json.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Implements the JSON-subset serialization and parsing surface of <see cref="NotableDateDocumentBuilder" />.
/// </summary>
/// <remarks>
/// <para>
/// The JSON form mirrors the companion JSON schema, which omits imports, restricts calendars to Gregorian, and exposes
/// only the reduced trigger, action, and override surface. Serializing a document that uses a feature outside that
/// subset throws <see cref="NotSupportedException" /> naming the offending feature.
/// </para>
/// </remarks>
public sealed partial class NotableDateDocumentBuilder
{
    /// <summary>The trigger types representable in the JSON subset.</summary>
    private static readonly HashSet<AdjustmentTrigger> s_jsonTriggers = new()
    {
        AdjustmentTrigger.Always,
        AdjustmentTrigger.IfDayOfWeek,
        AdjustmentTrigger.IfWeekend,
        AdjustmentTrigger.IfWeekday,
        AdjustmentTrigger.IfNonWorkingDay,
        AdjustmentTrigger.IfWorkingDay,
    };

    /// <summary>The action types representable in the JSON subset.</summary>
    private static readonly HashSet<AdjustmentAction> s_jsonActions = new()
    {
        AdjustmentAction.None,
        AdjustmentAction.AddDays,
        AdjustmentAction.MoveToNextWeekday,
        AdjustmentAction.MoveToPreviousWeekday,
        AdjustmentAction.MoveToNextWorkingDay,
        AdjustmentAction.MoveToPreviousWorkingDay,
        AdjustmentAction.Suppress,
    };

    /// <summary>
    /// Serializes the document to a <see cref="JsonObject" /> in the JSON-subset form.
    /// </summary>
    /// <returns>The serialized JSON object.</returns>
    /// <exception cref="InvalidOperationException">The document is incomplete.</exception>
    /// <exception cref="NotSupportedException">
    /// The document uses a feature the JSON subset cannot represent.
    /// </exception>
    public JsonObject ToJsonObject()
    {
        string resourceId = RequireResourceId();

        if (_imports.Count > 0)
            throw UnsupportedJsonFeature("imports");

        JsonObject root = new()
        {
            ["schemaVersion"] = _schemaVersion,
            ["resourceId"] = resourceId,
        };

        JsonObject? metadata = BuildMetadataJson();
        if (metadata is not null) root["metadata"] = metadata;

        JsonObject? resolutionPolicy = BuildResolutionPolicyJson();
        if (resolutionPolicy is not null) root["resolutionPolicy"] = resolutionPolicy;

        if (_adjustmentPolicies.Count > 0)
        {
            JsonArray policies = new();
            foreach (AdjustmentPolicyBuilder policy in _adjustmentPolicies)
                policies.Add(BuildAdjustmentPolicyJson(policy));

            root["adjustmentPolicies"] = policies;
        }

        JsonArray notableDates = new();
        foreach (NotableDateDefinitionBuilder definition in _definitions)
            notableDates.Add(BuildNotableDateJson(definition));

        root["notableDates"] = notableDates;

        if (_overrides is not null && _overrides.Entries.Count > 0)
        {
            JsonArray overrides = new();
            foreach (OverrideEntry entry in _overrides.Entries)
                overrides.Add(BuildOverrideJson(entry));

            root["overrides"] = overrides;
        }

        return root;
    }

    /// <summary>
    /// Serializes the document to an indented JSON string in the JSON-subset form.
    /// </summary>
    /// <returns>The serialized JSON.</returns>
    /// <exception cref="InvalidOperationException">The document is incomplete.</exception>
    /// <exception cref="NotSupportedException">
    /// The document uses a feature the JSON subset cannot represent.
    /// </exception>
    public string ToJson()
    {
        JsonObject root = ToJsonObject();
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream, new JsonWriterOptions { Indented = true }))
            root.WriteTo(writer);

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Parses a JSON string into a builder.
    /// </summary>
    /// <param name="json">The notable-date document JSON content.</param>
    /// <returns>A new <see cref="NotableDateDocumentBuilder" /> populated from the JSON.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="json" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="FormatException">
    /// <paramref name="json" /> is not well-formed or is not a notable-date document, an enumeration, integer, Boolean,
    /// date, month, trigger-month or <c>workingDays</c> value does not parse, a property that holds a value holds an
    /// object or an array, a property that holds an object or a list holds a value of another kind, an item of a list
    /// of identifiers, territory codes or tags is not a string, a rule's <c>strategy</c> and <c>recurrence</c>, or a
    /// <c>PatchRule</c> override's when it carries either, name no recognized kind or more than one between them, an
    /// <c>untilDate</c> declares no end strategy or one that names no recognized kind or more than one, an override's
    /// <c>operation</c> is absent or names no operation, or an <c>AddRule</c> override has no <c>rule</c>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// <paramref name="json" /> declares a field the builder's document model cannot hold: an empty <c>tags</c> or
    /// <c>adjustments</c> list on a <c>PatchRule</c> override, an empty <c>adjustments</c> list on an import use, a
    /// scope rule reference that does not name its <c>notableDateRef</c>, a handler parameter whose value is an object
    /// or an array, or a metadata name, description or source, an adjustment policy's description or a rule's comment
    /// that is an object or an array.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Enumeration values and the working week are read with the loader's rules, so they never parse into data the
    /// loader would read differently: an enumeration value must name a member (ignoring case; a number is not a name),
    /// and <c>workingDays</c> must be the seven-character, Sunday-first binary pattern. Any other value throws a
    /// <see cref="FormatException" /> carrying the message of the loader's <c>BODU-CAL-ENUM</c> or
    /// <c>BODU-CAL-WORKING-DAYS</c> diagnostic.
    /// </para>
    /// <para>
    /// Integers, Booleans and an adjustment trigger's month are read with the loader's rules too: a present value must
    /// be a 32-bit integer, an XML-schema Boolean literal (<c>true</c>, <c>false</c>, <c>1</c> or <c>0</c>), or a month
    /// name or number, and any other value throws a <see cref="FormatException" /> carrying the message of the loader's
    /// <c>BODU-CAL-VALUE</c> diagnostic.
    /// </para>
    /// <para>
    /// The attribute values of a strategy, a recurrence and an end strategy are read the same way, each as the loader
    /// reads it: an integer, a Boolean, a weekday, week ordinal, direction or invalid-day behavior, a <c>yyyy-MM-dd</c>
    /// date, and a month, a fixed strategy's in the calendar the rule's applicability names. A value the loader rejects
    /// throws a <see cref="FormatException" /> carrying the message of its <c>BODU-CAL-VALUE</c>, <c>BODU-CAL-ENUM</c>
    /// or <c>BODU-CAL-MONTH</c> diagnostic, and a value it reads is written in the form the schema accepts: a name in
    /// another case as the member's name, a number in quotes by its digits, and a month as its English name, or a fixed
    /// month in a calendar other than the Gregorian as its number or its month alias.
    /// </para>
    /// <para>
    /// Text is read as the loader reads it: a string verbatim, a number by its invariant digits, and a Boolean as
    /// <c>true</c> or <c>false</c>, so <c>"displayName": 5</c> reads as the text <c>5</c>. An item of a list of
    /// identifiers, territory codes or tags must be a string, and any other item throws a
    /// <see cref="FormatException" /> carrying the message of the loader's <c>BODU-CAL-VALUE</c> diagnostic; an item of
    /// a list of enumeration names is read by its JSON text, so a number there throws as a value that names no member,
    /// while an object or an array in a list of names or integers throws as a value of the wrong kind. The metadata, an
    /// adjustment policy's description and a rule's comment, which the loader does not read, accept the same scalars.
    /// Any other property that holds a value, holding an object or an array, and a property that holds an object or a
    /// list, such as <c>applicability</c> or <c>tags</c>, holding a value of another kind, throw a
    /// <see cref="FormatException" /> carrying the message of the loader's <c>BODU-CAL-VALUE</c> diagnostic, while the
    /// JSON literal <c>null</c> reads as absent, as the loader reads it. The message names a value or an item of the
    /// wrong kind by its JSON kind (<c>object</c>, <c>array</c>, <c>number</c>, <c>Boolean</c> or <c>null</c>), as the
    /// loader's does, rather than quoting it.
    /// </para>
    /// <para>
    /// Every field the loader's <c>LoadJson</c> reads is read, including those outside the JSON subset that
    /// <see cref="ToJson()" /> writes (imports, the trigger and action refinements, handler parameters, scope rule
    /// references and year bounds, and a <c>PatchRule</c> override's applicability, strategy, recurrence, duration,
    /// tags and adjustments), so the builder and the loader read the same document into the same resource. Property
    /// names are matched ignoring case, as the loader matches them, so <c>"Strategy"</c> reads as <c>"strategy"</c>,
    /// and each strategy and recurrence attribute is written under its schema name, while a property the loader does
    /// not read is ignored, as the loader ignores it. The few fields the builder's model cannot hold throw a
    /// <see cref="NotSupportedException" /> rather than being dropped. A rule's <c>strategy</c> and <c>recurrence</c>,
    /// and a <c>PatchRule</c> override's when it carries either, must name exactly one recognized kind between them,
    /// matched ignoring case as the loader matches it, and an <c>untilDate</c> must declare an end strategy that names
    /// exactly one, or the document throws a <see cref="FormatException" /> carrying the message of the loader's
    /// <c>BODU-CAL-RULE-SOURCE</c>, <c>BODU-CAL-DURATION-NOEND</c> or <c>BODU-CAL-DURATION-CONFLICT</c> diagnostic,
    /// rather than building the first kind alone or none. An override's <c>operation</c> is matched ignoring case, as
    /// the loader matches it; one that is absent or names no operation throws a <see cref="FormatException" /> carrying
    /// the message of the loader's <c>BODU-CAL-ENUM</c> diagnostic, rather than being read as a <c>PatchRule</c>, and
    /// an <c>AddRule</c> override without a <c>rule</c> throws one carrying the message of its <c>BODU-CAL-SCHEMA</c>
    /// diagnostic, rather than adding a rule with an empty identifier.
    /// </para>
    /// <para>
    /// Parsing does not otherwise validate the document: a document the loader would reject for a reason other than a
    /// malformed value, such as a dangling reference, can still parse. Validate with <see cref="Validate()" /> or
    /// <see cref="TryBuild(out NotableDateResource?, out IReadOnlyList{NotableDateValidationDiagnostic})" />, or load
    /// the text with <see cref="NotableDateResourceLoader" />, when the input is not trusted.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Round-trip a document through its JSON-subset form, then materialize a resource.
    /// NotableDateDocumentBuilder original = NotableDateDocumentBuilder.Create("contoso-holidays")
    ///     .AddNotableDate("new-year", "New Year's Day", NotableDateCategory.PublicHoliday, c => c
    ///         .AddRule("fixed", r => r.Fixed(1, 1)));
    /// string json = original.ToJson();
    ///
    /// NotableDateDocumentBuilder reparsed = NotableDateDocumentBuilder.FromJson(json);
    /// NotableDateResource resource = reparsed.Build();
    ///]]>
    /// </code>
    /// </example>
    public static NotableDateDocumentBuilder FromJson(string json)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(json);

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new FormatException(string.Format(CultureInfo.CurrentCulture, BuilderResourceStrings.Format_Invalid_JsonNotWellFormed, ex.Message), ex);
        }

        if (node is not JsonObject root)
            throw new FormatException(BuilderResourceStrings.Format_Invalid_JsonRoot);

        return FromJsonObject(root);
    }

    /// <summary>
    /// Parses a <see cref="JsonObject" /> into a builder.
    /// </summary>
    /// <param name="json">The notable-date document JSON object.</param>
    /// <returns>A new <see cref="NotableDateDocumentBuilder" /> populated from the object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">
    /// An enumeration, integer, Boolean, date, month, trigger-month or <c>workingDays</c> value does not parse, a
    /// property that holds a value holds an object or an array, a property that holds an object or a list holds a value
    /// of another kind, an item of a list of identifiers, territory codes or tags is not a string, a rule's
    /// <c>strategy</c> and <c>recurrence</c>, or a <c>PatchRule</c> override's when it carries either, name no
    /// recognized kind or more than one between them, an <c>untilDate</c> declares no end strategy or one that names no
    /// recognized kind or more than one, an override's <c>operation</c> is absent or names no operation, or an
    /// <c>AddRule</c> override has no <c>rule</c>.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// <paramref name="json" /> declares a field the builder's document model cannot hold; see
    /// <see cref="FromJson(string)" />.
    /// </exception>
    /// <remarks>
    /// Values are read as <see cref="FromJson(string)" /> reads them; see its remarks.
    /// </remarks>
    public static NotableDateDocumentBuilder FromJsonObject(JsonObject json)
    {
        ThrowHelper.ThrowIfNull(json);

        NotableDateDocumentBuilder builder = new()
        {
            _resourceId = ScalarText(json, "resourceId"),
            _schemaVersion = ScalarText(json, "schemaVersion") ?? BuilderXml.DefaultSchemaVersion,
        };

        builder.ReadMetadataJson(Property(json, "metadata") as JsonObject);
        builder.ReadResolutionPolicyJson(ObjectProperty(json, "resolutionPolicy"));
        builder.ReadAdjustmentPoliciesJson(ArrayProperty(json, "adjustmentPolicies"));
        builder.ReadImportsJson(ArrayProperty(json, "imports"));
        builder.ReadNotableDatesJson(ArrayProperty(json, "notableDates"));
        builder.ReadOverridesJson(ArrayProperty(json, "overrides"));

        return builder;
    }

    /// <summary>
    /// Creates a <see cref="NotSupportedException" /> describing a feature the JSON subset cannot represent.
    /// </summary>
    /// <param name="feature">The human-readable name of the unsupported feature.</param>
    /// <returns>The exception to throw.</returns>
    private static NotSupportedException UnsupportedJsonFeature(string feature) =>
        new(string.Format(CultureInfo.CurrentCulture, BuilderResourceStrings.Op_NotSupported_JsonFeature, feature));

    /// <summary>
    /// Builds the JSON <c>metadata</c> object when any metadata value is configured.
    /// </summary>
    /// <returns>The metadata object, or <see langword="null" /> when none is configured.</returns>
    private JsonObject? BuildMetadataJson()
    {
        if (_metadataName is null && _metadataDescription is null && _sources.Count == 0)
            return null;

        JsonObject metadata = new();
        if (_metadataName is not null) metadata["name"] = _metadataName;
        if (_metadataDescription is not null) metadata["description"] = _metadataDescription;
        if (_sources.Count > 0)
        {
            JsonArray sources = new();
            foreach (string source in _sources)
                sources.Add(source);

            metadata["sources"] = sources;
        }

        return metadata;
    }

    /// <summary>
    /// Builds the JSON <c>resolutionPolicy</c> object when any policy value is configured.
    /// </summary>
    /// <returns>The resolution-policy object, or <see langword="null" /> when none is configured.</returns>
    private JsonObject? BuildResolutionPolicyJson()
    {
        if (_resolutionPolicy is null || !_resolutionPolicy.HasAnyValue())
            return null;

        ResolutionPolicyBuilder policy = _resolutionPolicy;
        JsonObject result = new();
        if (policy.DuplicatePolicy is DuplicatePolicy duplicatePolicy) result["duplicatePolicy"] = duplicatePolicy.ToString();
        if (policy.SameDayCollisionPolicy is CollisionPolicy sameDay) result["sameDayCollisionPolicy"] = sameDay.ToString();
        if (policy.SpanCollisionPolicy is CollisionPolicy span) result["spanCollisionPolicy"] = span.ToString();
        if (policy.PriorityDirection is PriorityDirection direction) result["priorityDirection"] = direction.ToString();
        if (policy.ObservedDateRangePolicy is ObservedDateRangePolicy observed) result["observedDateRangePolicy"] = observed.ToString();
        if (policy.WorkingWeek is DayOfWeekSet week) result["workingDays"] = week.ToString("01", CultureInfo.InvariantCulture);

        if (policy.CategoryPrecedence is { Count: > 0 } precedence)
        {
            JsonArray array = new();
            foreach (NotableDateCategory category in precedence)
                array.Add(category.ToString());

            result["categoryPrecedence"] = array;
        }

        return result;
    }

    /// <summary>
    /// Builds the JSON object for a single adjustment policy, validating that it lies within the JSON subset.
    /// </summary>
    /// <param name="policy">The policy to serialize.</param>
    /// <returns>The serialized policy object.</returns>
    /// <exception cref="InvalidOperationException">The policy is missing a trigger, action, or emission.</exception>
    /// <exception cref="NotSupportedException">The policy uses a feature the JSON subset cannot represent.</exception>
    private static JsonObject BuildAdjustmentPolicyJson(AdjustmentPolicyBuilder policy)
    {
        policy.EnsureComplete();

        if (!s_jsonTriggers.Contains(policy.TriggerType!.Value))
            throw UnsupportedJsonFeature(string.Format(CultureInfo.InvariantCulture, "adjustment trigger '{0}'", policy.TriggerType.Value));

        if (!s_jsonActions.Contains(policy.ActionType!.Value))
            throw UnsupportedJsonFeature(string.Format(CultureInfo.InvariantCulture, "adjustment action '{0}'", policy.ActionType.Value));

        if (policy.TriggerMonth is not null) throw UnsupportedJsonFeature("adjustment trigger month");
        if (policy.TriggerDay is not null) throw UnsupportedJsonFeature("adjustment trigger day");
        if (policy.TriggerWeekOrdinal is not null) throw UnsupportedJsonFeature("adjustment trigger weekOrdinal");
        if (policy.TriggerHandlerKey is not null) throw UnsupportedJsonFeature("adjustment trigger handlerKey");
        if (policy.ActionNotableDateRef is not null) throw UnsupportedJsonFeature("adjustment action notableDateRef");
        if (policy.ActionRuleRef is not null) throw UnsupportedJsonFeature("adjustment action ruleRef");
        if (policy.ActionHandlerKey is not null) throw UnsupportedJsonFeature("adjustment action handlerKey");
        if (policy.Parameters.Count > 0) throw UnsupportedJsonFeature("adjustment handler parameters");

        JsonObject result = new() { ["id"] = policy.Id };
        if (policy.Priority is int priority) result["priority"] = priority;
        if (policy.Description is not null) result["description"] = policy.Description;

        if (policy.Scope is AdjustmentScopeBuilder scope && scope.HasAnyValue)
            result["scope"] = BuildScopeJson(scope);

        JsonObject trigger = new() { ["type"] = policy.TriggerType.Value.ToString() };
        if (policy.TriggerWeekdays.Count > 0)
        {
            JsonArray weekdays = new();
            foreach (DayOfWeek day in policy.TriggerWeekdays)
                weekdays.Add(day.ToString());

            trigger["weekdays"] = weekdays;
        }

        result["trigger"] = trigger;

        JsonObject action = new() { ["type"] = policy.ActionType.Value.ToString() };
        if (policy.ActionDays is int days) action["days"] = days;
        if (policy.ActionDayOfWeek is DayOfWeek dayOfWeek) action["dayOfWeek"] = dayOfWeek.ToString();
        if (policy.ActionMaxSearchDays is int maxSearchDays) action["maxSearchDays"] = maxSearchDays;
        if (policy.ActionSkipWeekends is bool skipWeekends) action["skipWeekends"] = skipWeekends;
        if (policy.ActionSkipNonWorkingDates is bool skipNonWorking) action["skipNonWorkingDates"] = skipNonWorking;
        result["action"] = action;

        JsonObject emission = new() { ["mode"] = policy.EmissionModeValue!.Value.ToString() };
        if (policy.EmissionReason is not null) emission["reason"] = policy.EmissionReason;
        if (policy.EmissionNonWorking is bool emissionNonWorking) emission["nonWorking"] = emissionNonWorking;
        result["emission"] = emission;

        return result;
    }

    /// <summary>
    /// Builds the JSON object for an adjustment scope, validating that it lies within the JSON subset.
    /// </summary>
    /// <param name="scope">The scope to serialize.</param>
    /// <returns>The serialized scope object.</returns>
    /// <exception cref="NotSupportedException">The scope uses a feature the JSON subset cannot represent.</exception>
    private static JsonObject BuildScopeJson(AdjustmentScopeBuilder scope)
    {
        if (scope.FromYearValue is not null || scope.ToYearValue is not null) throw UnsupportedJsonFeature("adjustment scope year bounds");
        if (scope.OnlyYears.Count > 0) throw UnsupportedJsonFeature("adjustment scope OnlyYear");
        if (scope.ExceptYears.Count > 0) throw UnsupportedJsonFeature("adjustment scope ExceptYear");
        if (scope.NotableDateRefs.Count > 0) throw UnsupportedJsonFeature("adjustment scope NotableDate reference");
        if (scope.RuleRefs.Count > 0) throw UnsupportedJsonFeature("adjustment scope Rule reference");

        JsonObject result = new();
        if (scope.Territories.Count > 0)
        {
            JsonArray territories = new();
            foreach (string territory in scope.Territories)
                territories.Add(territory);

            result["territories"] = territories;
        }

        if (scope.Calendars.Count > 0)
        {
            JsonArray calendars = new();
            foreach (CalendarSystem calendar in scope.Calendars)
                calendars.Add(calendar.ToString());

            result["calendars"] = calendars;
        }

        if (scope.Categories.Count > 0)
        {
            JsonArray categories = new();
            foreach (NotableDateCategory category in scope.Categories)
                categories.Add(category.ToString());

            result["categories"] = categories;
        }

        return result;
    }

    /// <summary>
    /// Builds the JSON object for a single notable-date concept.
    /// </summary>
    /// <param name="definition">The concept to serialize.</param>
    /// <returns>The serialized concept object.</returns>
    /// <exception cref="InvalidOperationException">The concept has no rules or a rule has no strategy.</exception>
    /// <exception cref="NotSupportedException">A rule uses a feature the JSON subset cannot represent.</exception>
    private static JsonObject BuildNotableDateJson(NotableDateDefinitionBuilder definition)
    {
        if (definition.Rules.Count == 0)
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, BuilderResourceStrings.Op_Invalid_DefinitionHasNoRules, definition.Id));

        JsonObject result = new()
        {
            ["id"] = definition.Id,
            ["displayName"] = definition.DisplayName,
            ["category"] = definition.Category.ToString(),
        };

        if (definition.DefaultDurationDays is int durationDays) result["defaultDurationDays"] = durationDays;
        if (definition.DefaultNonWorkingDay is bool nonWorking) result["defaultNonWorkingDay"] = nonWorking;
        if (definition.Tags.Count > 0) result["tags"] = BuildStringArray(definition.Tags);

        JsonArray rules = new();
        foreach (NotableDateRuleBuilder rule in definition.Rules)
            rules.Add(BuildRuleJson(rule, requireStrategy: true));

        result["rules"] = rules;
        return result;
    }

    /// <summary>
    /// Builds the JSON object for a single rule, validating that it lies within the JSON subset.
    /// </summary>
    /// <param name="rule">The rule to serialize.</param>
    /// <param name="requireStrategy">A value indicating whether the rule must declare a strategy.</param>
    /// <returns>The serialized rule object.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="requireStrategy" /> is <see langword="true" /> and the rule has no strategy.
    /// </exception>
    /// <exception cref="NotSupportedException">The rule uses a non-Gregorian calendar.</exception>
    private static JsonObject BuildRuleJson(NotableDateRuleBuilder rule, bool requireStrategy)
    {
        if (requireStrategy && rule.Strategy is null && rule.Recurrence is null)
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, BuilderResourceStrings.Op_Invalid_RuleStrategyNotSet, rule.Id));

        if (rule.Calendar is CalendarSystem calendar && calendar != CalendarSystem.Gregorian)
            throw UnsupportedJsonFeature(string.Format(CultureInfo.InvariantCulture, "calendar '{0}'", calendar));

        JsonObject result = new() { ["id"] = rule.Id };
        if (rule.Priority is int priority) result["priority"] = priority;
        if (rule.Category is NotableDateCategory category) result["category"] = category.ToString();
        if (rule.NonWorking is bool nonWorking) result["nonWorking"] = nonWorking;
        if (rule.DurationDays is int durationDays) result["durationDays"] = durationDays;
        if (rule.Comment is not null) result["comment"] = rule.Comment;

        if (rule.HasApplicability)
            result["applicability"] = BuildApplicabilityJson(rule);

        if (rule.Strategy is not null)
            result["strategy"] = BuildStrategyJson(rule.Strategy);

        if (rule.Recurrence is not null)
            result["recurrence"] = BuildRecurrenceJson(rule.Recurrence);

        if (rule.EndStrategy is not null)
        {
            result["duration"] = new JsonObject
            {
                ["untilDate"] = new JsonObject
                {
                    ["startBoundary"] = rule.EndStartBoundary.ToString(),
                    ["endBoundary"] = rule.EndEndBoundary.ToString(),
                    ["selection"] = rule.EndSelectionValue.ToString(),
                    ["strategy"] = BuildStrategyJson(rule.EndStrategy),
                },
            };
        }

        if (rule.Tags.Count > 0) result["tags"] = BuildStringArray(rule.Tags);
        if (rule.Adjustments.Count > 0) result["adjustments"] = BuildStringArray(rule.Adjustments);

        return result;
    }

    /// <summary>
    /// Builds the JSON <c>recurrence</c> object from a recurrence element, mapping its kind name and attributes.
    /// </summary>
    /// <param name="recurrence">The <c>Recurrence</c> element.</param>
    /// <returns>The serialized recurrence object.</returns>
    private static JsonObject BuildRecurrenceJson(XElement recurrence)
    {
        XElement kind = recurrence.Elements().First();
        JsonObject inner = new();

        foreach (XAttribute attribute in kind.Attributes())
        {
            inner[attribute.Name.LocalName] = attribute.Name.LocalName switch
            {
                "intervalDays" or "intervalWeeks" or "intervalMonths" or "dayOfMonth" => JsonValue.Create(int.Parse(attribute.Value, CultureInfo.InvariantCulture)),
                _ => JsonValue.Create(attribute.Value),
            };
        }

        if (kind.Name.LocalName == "Weekly")
        {
            JsonArray days = new();
            foreach (XElement day in kind.Elements(BuilderXml.Namespace + "Day"))
                days.Add(JsonValue.Create((string?)day.Attribute("dayOfWeek") ?? string.Empty));
            inner["days"] = days;
        }

        string key = kind.Name.LocalName switch
        {
            "DailyInterval" => "dailyInterval",
            "Weekly" => "weekly",
            "MonthlyDay" => "monthlyDay",
            _ => "monthlyWeekday",
        };

        return new JsonObject { [key] = inner };
    }

    /// <summary>
    /// Builds the JSON <c>applicability</c> object from a rule's applicability state.
    /// </summary>
    /// <param name="rule">The rule supplying the applicability values.</param>
    /// <returns>The serialized applicability object.</returns>
    private static JsonObject BuildApplicabilityJson(NotableDateRuleBuilder rule)
    {
        JsonObject result = new();
        if (rule.Calendar is CalendarSystem calendar) result["calendar"] = calendar.ToString();
        if (rule.FromYearValue is int fromYear) result["fromYear"] = fromYear;
        if (rule.ToYearValue is int toYear) result["toYear"] = toYear;
        if (rule.EveryYearsValue is int everyYears) result["everyYears"] = everyYears;
        if (rule.AnchorYearValue is int anchorYear) result["anchorYear"] = anchorYear;
        if (rule.Territories.Count > 0) result["territories"] = BuildStringArray(rule.Territories);
        if (rule.OnlyYearsValues.Count > 0) result["onlyYears"] = BuildIntArray(rule.OnlyYearsValues);
        if (rule.ExceptYearsValues.Count > 0) result["exceptYears"] = BuildIntArray(rule.ExceptYearsValues);

        return result;
    }

    /// <summary>
    /// Builds the JSON <c>strategy</c> object from a strategy element, mapping its element name and attributes.
    /// </summary>
    /// <param name="strategy">The strategy element.</param>
    /// <returns>The serialized strategy object.</returns>
    private static JsonObject BuildStrategyJson(XElement strategy)
    {
        JsonObject inner = new();
        foreach (XAttribute attribute in strategy.Attributes())
        {
            inner[attribute.Name.LocalName] = attribute.Name.LocalName switch
            {
                "day" or "offsetDays" or "ordinal" or "week" or "offsetWorkingDays" or "referenceYearOffset" => JsonValue.Create(int.Parse(attribute.Value, CultureInfo.InvariantCulture)),
                "skipLeapMonth" or "sweepCalendarYears" => JsonValue.Create(BuilderXml.ParseBool(attribute.Value) ?? false),
                _ => JsonValue.Create(attribute.Value),
            };
        }

        string key = strategy.Name.LocalName switch
        {
            "Fixed" => "fixed",
            "DayOfWeekInMonth" => "dayOfWeekInMonth",
            "WeekdayNearDate" => "weekdayNearDate",
            "RelativeWeekdayInMonth" => "relativeWeekdayInMonth",
            "OffsetFromRule" => "offsetFromRule",
            "WeekdayNearRule" => "weekdayNearRule",
            "NthWeekdayFromRule" => "nthWeekdayFromRule",
            "WorkingDayOffsetFromRule" => "workingDayOffsetFromRule",
            "WorkingDayInMonth" => "workingDayInMonth",
            "OrdinalDayOfMonth" => "ordinalDayOfMonth",
            "IsoWeekDate" => "isoWeekDate",
            "DayOfYear" => "dayOfYear",
            _ => "algorithm",
        };

        return new JsonObject { [key] = inner };
    }

    /// <summary>
    /// Builds the JSON object for a single override operation in the flat override shape.
    /// </summary>
    /// <param name="entry">The override operation to serialize.</param>
    /// <returns>The serialized override object.</returns>
    /// <exception cref="InvalidOperationException">An added override rule has no strategy.</exception>
    /// <exception cref="NotSupportedException">
    /// A patch override configures a section the flat JSON override cannot represent.
    /// </exception>
    private static JsonObject BuildOverrideJson(OverrideEntry entry)
    {
        JsonObject result = new()
        {
            ["operation"] = entry.Operation.ToString(),
            ["notableDateRef"] = entry.NotableDateRef,
        };

        switch (entry.Operation)
        {
            case OverrideOperation.AddRule:
                result["rule"] = BuildRuleJson(entry.Rule!, requireStrategy: true);
                break;

            case OverrideOperation.RemoveRule:
                result["ruleRef"] = entry.RuleRef!;
                break;

            default:
                WritePatchJson(result, entry);
                break;
        }

        return result;
    }

    /// <summary>
    /// Writes the flat patch scalars for a patch override, rejecting sections the JSON override cannot represent.
    /// </summary>
    /// <param name="result">The override object receiving the patch scalars.</param>
    /// <param name="entry">The patch override operation.</param>
    /// <exception cref="NotSupportedException">
    /// The patch configures applicability, a strategy, a recurrence, a duration end, tags, or adjustments.
    /// </exception>
    private static void WritePatchJson(JsonObject result, OverrideEntry entry)
    {
        NotableDateRuleBuilder rule = entry.Rule!;
        if (rule.HasApplicability) throw UnsupportedJsonFeature("override patch applicability");
        if (rule.Strategy is not null) throw UnsupportedJsonFeature("override patch strategy");
        if (rule.Recurrence is not null) throw UnsupportedJsonFeature("override patch recurrence");
        if (rule.EndStrategy is not null) throw UnsupportedJsonFeature("override patch duration");
        if (rule.Tags.Count > 0) throw UnsupportedJsonFeature("override patch tags");
        if (rule.Adjustments.Count > 0) throw UnsupportedJsonFeature("override patch adjustments");

        result["ruleRef"] = entry.RuleRef!;
        if (rule.Priority is int priority) result["priority"] = priority;
        if (rule.Category is NotableDateCategory category) result["category"] = category.ToString();
        if (rule.NonWorking is bool nonWorking) result["nonWorking"] = nonWorking;
        if (rule.DurationDays is int durationDays) result["durationDays"] = durationDays;
    }

    /// <summary>
    /// Builds a JSON array of string values.
    /// </summary>
    /// <param name="values">The string values.</param>
    /// <returns>The JSON array.</returns>
    private static JsonArray BuildStringArray(IReadOnlyList<string> values)
    {
        JsonArray array = new();
        foreach (string value in values)
            array.Add(value);

        return array;
    }

    /// <summary>
    /// Builds a JSON array of integer values.
    /// </summary>
    /// <param name="values">The integer values.</param>
    /// <returns>The JSON array.</returns>
    private static JsonArray BuildIntArray(IReadOnlyList<int> values)
    {
        JsonArray array = new();
        foreach (int value in values)
            array.Add(value);

        return array;
    }
}
