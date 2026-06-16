// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilder.Xml.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Implements the full-fidelity XML serialization and parsing surface of <see cref="NotableDateDocumentBuilder" />.
/// </summary>
public sealed partial class NotableDateDocumentBuilder
{
    /// <summary>
    /// Serializes the document to an <see cref="XDocument" /> in the <c>urn:bodu:globalization:calendar</c> namespace.
    /// </summary>
    /// <returns>The serialized document.</returns>
    /// <exception cref="InvalidOperationException">The document is incomplete.</exception>
    public XDocument ToXDocument()
    {
        string resourceId = RequireResourceId();

        XElement root = new(
            BuilderXml.Namespace + "NotableDateResource",
            new XAttribute("schemaVersion", _schemaVersion),
            new XAttribute("resourceId", resourceId));

        XElement? metadata = BuildMetadataElement();
        if (metadata is not null) root.Add(metadata);

        XElement? resolutionPolicy = BuildResolutionPolicyElement();
        if (resolutionPolicy is not null) root.Add(resolutionPolicy);

        XElement? adjustmentPolicies = BuildAdjustmentPoliciesElement();
        if (adjustmentPolicies is not null) root.Add(adjustmentPolicies);

        XElement? imports = BuildImportsElement();
        if (imports is not null) root.Add(imports);

        XElement? notableDates = BuildNotableDatesElement();
        if (notableDates is not null) root.Add(notableDates);

        XElement? overrides = BuildOverridesElement();
        if (overrides is not null) root.Add(overrides);

        return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
    }

    /// <summary>
    /// Serializes the document to an indented XML string in the <c>urn:bodu:globalization:calendar</c> namespace.
    /// </summary>
    /// <returns>The serialized XML.</returns>
    /// <exception cref="InvalidOperationException">The document is incomplete.</exception>
    public string ToXml()
    {
        XDocument document = ToXDocument();
        XmlWriterSettings settings = new()
        {
            Indent = true,
            Encoding = new System.Text.UTF8Encoding(false),
            OmitXmlDeclaration = false,
        };

        using var stringWriter = new System.IO.StringWriter(CultureInfo.InvariantCulture);
        using (var writer = XmlWriter.Create(stringWriter, settings))
            document.Save(writer);

        return stringWriter.ToString();
    }

    /// <summary>
    /// Parses an XML document into a builder.
    /// </summary>
    /// <param name="xml">The notable-date document XML content.</param>
    /// <returns>A new <see cref="NotableDateDocumentBuilder" /> populated from the XML.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="xml" /> is <see langword="null" />, empty, or white-space.
    /// </exception>
    /// <exception cref="FormatException">
    /// <paramref name="xml" /> is not well-formed or is not a notable-date document.
    /// </exception>
    public static NotableDateDocumentBuilder FromXml(string xml)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(xml);

        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (XmlException ex)
        {
            throw new FormatException(string.Format(CultureInfo.CurrentCulture, BuilderResourceStrings.Format_Invalid_XmlNotWellFormed, ex.Message), ex);
        }

        return FromXDocument(document);
    }

    /// <summary>
    /// Parses an <see cref="XDocument" /> into a builder.
    /// </summary>
    /// <param name="document">The notable-date document.</param>
    /// <returns>A new <see cref="NotableDateDocumentBuilder" /> populated from the document.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="document" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">The root element is not a notable-date resource element.</exception>
    public static NotableDateDocumentBuilder FromXDocument(XDocument document)
    {
        ThrowHelper.ThrowIfNull(document);

        XElement? root = document.Root;
        if (root is null || root.Name != BuilderXml.Namespace + "NotableDateResource")
            throw new FormatException(string.Format(CultureInfo.CurrentCulture, BuilderResourceStrings.Format_Invalid_RootElement, root?.Name.LocalName ?? string.Empty));

        NotableDateDocumentBuilder builder = new()
        {
            _resourceId = (string?)root.Attribute("resourceId"),
            _schemaVersion = (string?)root.Attribute("schemaVersion") ?? BuilderXml.DefaultSchemaVersion,
        };

        builder.ReadMetadata(root.Element(BuilderXml.Namespace + "Metadata"));
        builder.ReadResolutionPolicy(root.Element(BuilderXml.Namespace + "ResolutionPolicy"));
        builder.ReadAdjustmentPolicies(root.Element(BuilderXml.Namespace + "AdjustmentPolicies"));
        builder.ReadImports(root.Element(BuilderXml.Namespace + "Imports"));
        builder.ReadNotableDates(root.Element(BuilderXml.Namespace + "NotableDates"));
        builder.ReadOverrides(root.Element(BuilderXml.Namespace + "Overrides"));

        return builder;
    }

    /// <summary>
    /// Builds the <c>Metadata</c> element when any metadata value is configured.
    /// </summary>
    /// <returns>The metadata element, or <see langword="null" /> when no metadata is configured.</returns>
    private XElement? BuildMetadataElement()
    {
        if (_metadataName is null && _metadataDescription is null && _sources.Count == 0)
            return null;

        XElement element = new(BuilderXml.Namespace + "Metadata");
        if (_metadataName is not null) element.Add(new XElement(BuilderXml.Namespace + "Name", _metadataName));
        if (_metadataDescription is not null) element.Add(new XElement(BuilderXml.Namespace + "Description", _metadataDescription));
        foreach (string source in _sources)
            element.Add(new XElement(BuilderXml.Namespace + "Source", source));

        return element;
    }

    /// <summary>
    /// Builds the <c>ResolutionPolicy</c> element when any policy value is configured.
    /// </summary>
    /// <returns>The resolution-policy element, or <see langword="null" /> when no policy is configured.</returns>
    private XElement? BuildResolutionPolicyElement()
    {
        if (_resolutionPolicy?.HasAnyValue() != true)
            return null;

        ResolutionPolicyBuilder policy = _resolutionPolicy;
        XElement element = new(BuilderXml.Namespace + "ResolutionPolicy");
        if (policy.DuplicatePolicy is DuplicatePolicy duplicatePolicy) element.SetAttributeValue("duplicatePolicy", duplicatePolicy.ToString());
        if (policy.SameDayCollisionPolicy is CollisionPolicy sameDay) element.SetAttributeValue("sameDayCollisionPolicy", sameDay.ToString());
        if (policy.SpanCollisionPolicy is CollisionPolicy span) element.SetAttributeValue("spanCollisionPolicy", span.ToString());
        if (policy.PriorityDirection is PriorityDirection direction) element.SetAttributeValue("priorityDirection", direction.ToString());
        if (policy.ObservedDateRangePolicy is ObservedDateRangePolicy observed) element.SetAttributeValue("observedDateRangePolicy", observed.ToString());
        if (policy.WorkingWeek is WeekPattern week) element.SetAttributeValue("workingDays", week.ToString("01", CultureInfo.InvariantCulture));

        if (policy.CategoryPrecedence is { Count: > 0 } precedence)
        {
            XElement precedenceElement = new(BuilderXml.Namespace + "CategoryPrecedence");
            foreach (NotableDateCategory category in precedence)
                precedenceElement.Add(new XElement(BuilderXml.Namespace + "Category", new XAttribute("value", category.ToString())));

            element.AddFirst(precedenceElement);
        }

        return element;
    }

    /// <summary>
    /// Builds the <c>AdjustmentPolicies</c> element when at least one policy is configured.
    /// </summary>
    /// <returns>The adjustment-policies element, or <see langword="null" /> when no policies are configured.</returns>
    /// <exception cref="InvalidOperationException">
    /// An adjustment policy is missing a trigger, action, or emission.
    /// </exception>
    private XElement? BuildAdjustmentPoliciesElement()
    {
        if (_adjustmentPolicies.Count == 0)
            return null;

        XElement element = new(BuilderXml.Namespace + "AdjustmentPolicies");
        foreach (AdjustmentPolicyBuilder policy in _adjustmentPolicies)
            element.Add(BuildAdjustmentPolicyElement(policy));

        return element;
    }

    /// <summary>
    /// Builds a single <c>AdjustmentPolicy</c> element, emitting its scope, trigger, action, emission, and parameters
    /// in schema order.
    /// </summary>
    /// <param name="policy">The policy to serialize.</param>
    /// <returns>The serialized policy element.</returns>
    /// <exception cref="InvalidOperationException">The policy is missing a trigger, action, or emission.</exception>
    private static XElement BuildAdjustmentPolicyElement(AdjustmentPolicyBuilder policy)
    {
        policy.EnsureComplete();

        XElement element = new(BuilderXml.Namespace + "AdjustmentPolicy", new XAttribute("id", policy.Id));
        if (policy.Priority is int priority) element.SetAttributeValue("priority", BuilderXml.Int(priority));
        if (policy.Description is not null) element.SetAttributeValue("description", policy.Description);

        if (policy.Scope is AdjustmentScopeBuilder scope && scope.HasAnyValue)
            element.Add(BuildScopeElement(scope));

        XElement trigger = new(BuilderXml.Namespace + "Trigger", new XAttribute("type", policy.TriggerType!.Value.ToString()));
        if (policy.TriggerMonth is int triggerMonth) trigger.SetAttributeValue("month", BuilderXml.GetMonthName(triggerMonth));
        if (policy.TriggerDay is int triggerDay) trigger.SetAttributeValue("day", BuilderXml.Int(triggerDay));
        if (policy.TriggerWeekOrdinal is WeekOrdinal weekOrdinal) trigger.SetAttributeValue("weekOrdinal", weekOrdinal.ToString());
        if (policy.TriggerHandlerKey is not null) trigger.SetAttributeValue("handlerKey", policy.TriggerHandlerKey);
        foreach (DayOfWeek day in policy.TriggerWeekdays)
            trigger.Add(new XElement(BuilderXml.Namespace + "Weekday", new XAttribute("value", day.ToString())));
        element.Add(trigger);

        XElement action = new(BuilderXml.Namespace + "Action", new XAttribute("type", policy.ActionType!.Value.ToString()));
        if (policy.ActionDays is int days) action.SetAttributeValue("days", BuilderXml.Int(days));
        if (policy.ActionDayOfWeek is DayOfWeek actionDayOfWeek) action.SetAttributeValue("dayOfWeek", actionDayOfWeek.ToString());
        if (policy.ActionMaxSearchDays is int maxSearchDays) action.SetAttributeValue("maxSearchDays", BuilderXml.Int(maxSearchDays));
        if (policy.ActionSkipWeekends is bool skipWeekends) action.SetAttributeValue("skipWeekends", BuilderXml.Bool(skipWeekends));
        if (policy.ActionSkipNonWorkingDates is bool skipNonWorking) action.SetAttributeValue("skipNonWorkingDates", BuilderXml.Bool(skipNonWorking));
        if (policy.ActionNotableDateRef is not null) action.SetAttributeValue("notableDateRef", policy.ActionNotableDateRef);
        if (policy.ActionRuleRef is not null) action.SetAttributeValue("ruleRef", policy.ActionRuleRef);
        if (policy.ActionHandlerKey is not null) action.SetAttributeValue("handlerKey", policy.ActionHandlerKey);
        element.Add(action);

        XElement emission = new(BuilderXml.Namespace + "Emission", new XAttribute("mode", policy.EmissionModeValue!.Value.ToString()));
        if (policy.EmissionReason is not null) emission.SetAttributeValue("reason", policy.EmissionReason);
        if (policy.EmissionNonWorking is bool emissionNonWorking) emission.SetAttributeValue("nonWorking", BuilderXml.Bool(emissionNonWorking));
        element.Add(emission);

        if (policy.Parameters.Count > 0)
        {
            XElement parameters = new(BuilderXml.Namespace + "Parameters");
            foreach (KeyValuePair<string, string> parameter in policy.Parameters)
                parameters.Add(new XElement(BuilderXml.Namespace + "Param", new XAttribute("key", parameter.Key), new XAttribute("value", parameter.Value)));

            element.Add(parameters);
        }

        return element;
    }

    /// <summary>
    /// Builds an adjustment-policy <c>Scope</c> element from a scope builder.
    /// </summary>
    /// <param name="scope">The scope to serialize.</param>
    /// <returns>The serialized scope element.</returns>
    private static XElement BuildScopeElement(AdjustmentScopeBuilder scope)
    {
        XElement element = new(BuilderXml.Namespace + "Scope");
        if (scope.FromYearValue is int fromYear) element.SetAttributeValue("fromYear", BuilderXml.Int(fromYear));
        if (scope.ToYearValue is int toYear) element.SetAttributeValue("toYear", BuilderXml.Int(toYear));

        foreach (string territory in scope.Territories)
            element.Add(new XElement(BuilderXml.Namespace + "Territory", new XAttribute("code", territory)));
        foreach (CalendarSystem calendar in scope.Calendars)
            element.Add(new XElement(BuilderXml.Namespace + "Calendar", new XAttribute("name", calendar.ToString())));
        foreach (NotableDateCategory category in scope.Categories)
            element.Add(new XElement(BuilderXml.Namespace + "Category", new XAttribute("value", category.ToString())));
        foreach (string notableDateRef in scope.NotableDateRefs)
            element.Add(new XElement(BuilderXml.Namespace + "NotableDate", new XAttribute("ref", notableDateRef)));
        foreach ((string? notableDateRef, string? ruleRef) in scope.RuleRefs)
            element.Add(new XElement(BuilderXml.Namespace + "Rule", new XAttribute("notableDateRef", notableDateRef), new XAttribute("ruleRef", ruleRef)));
        foreach (int year in scope.OnlyYears)
            element.Add(new XElement(BuilderXml.Namespace + "OnlyYear", new XAttribute("value", BuilderXml.Int(year))));
        foreach (int year in scope.ExceptYears)
            element.Add(new XElement(BuilderXml.Namespace + "ExceptYear", new XAttribute("value", BuilderXml.Int(year))));

        return element;
    }

    /// <summary>
    /// Builds the <c>Imports</c> element when at least one import is configured.
    /// </summary>
    /// <returns>The imports element, or <see langword="null" /> when no imports are configured.</returns>
    private XElement? BuildImportsElement()
    {
        if (_imports.Count == 0)
            return null;

        XElement element = new(BuilderXml.Namespace + "Imports");
        foreach (ImportBuilder import in _imports)
        {
            XElement importElement = new(BuilderXml.Namespace + "Import", new XAttribute("resource", import.Resource));
            foreach (ImportUseBuilder use in import.Uses)
                importElement.Add(BuildImportUseElement(use));

            element.Add(importElement);
        }

        return element;
    }

    /// <summary>
    /// Builds a single import <c>Use</c> element from an import-use builder.
    /// </summary>
    /// <param name="use">The import-use directive to serialize.</param>
    /// <returns>The serialized use element.</returns>
    private static XElement BuildImportUseElement(ImportUseBuilder use)
    {
        XElement element = new(BuilderXml.Namespace + "Use", new XAttribute("notableDateRef", use.NotableDateRef));
        if (use.Alias is not null) element.SetAttributeValue("as", use.Alias);
        if (use.Territory is not null) element.SetAttributeValue("territory", use.Territory);
        if (use.Category is NotableDateCategory category) element.SetAttributeValue("category", category.ToString());
        if (use.NonWorking is bool nonWorking) element.SetAttributeValue("nonWorking", BuilderXml.Bool(nonWorking));

        if (use.Adjustments.Count > 0)
        {
            XElement adjustments = new(BuilderXml.Namespace + "Adjustments");
            foreach (string policyRef in use.Adjustments)
                adjustments.Add(new XElement(BuilderXml.Namespace + "Adjustment", new XAttribute("policyRef", policyRef)));

            element.Add(adjustments);
        }

        return element;
    }

    /// <summary>
    /// Builds the <c>NotableDates</c> element when at least one concept is configured.
    /// </summary>
    /// <returns>The notable-dates element, or <see langword="null" /> when no concepts are configured.</returns>
    /// <exception cref="InvalidOperationException">A concept has no rules or a rule has no strategy.</exception>
    private XElement? BuildNotableDatesElement()
    {
        if (_definitions.Count == 0)
            return null;

        XElement element = new(BuilderXml.Namespace + "NotableDates");
        foreach (NotableDateDefinitionBuilder definition in _definitions)
            element.Add(BuildNotableDateElement(definition));

        return element;
    }

    /// <summary>
    /// Builds a single <c>NotableDate</c> element, emitting its tags and rules in schema order.
    /// </summary>
    /// <param name="definition">The concept to serialize.</param>
    /// <returns>The serialized concept element.</returns>
    /// <exception cref="InvalidOperationException">The concept has no rules or a rule has no strategy.</exception>
    private static XElement BuildNotableDateElement(NotableDateDefinitionBuilder definition)
    {
        if (definition.Rules.Count == 0)
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, BuilderResourceStrings.Op_Invalid_DefinitionHasNoRules, definition.Id));

        XElement element = new(
            BuilderXml.Namespace + "NotableDate",
            new XAttribute("id", definition.Id),
            new XAttribute("displayName", definition.DisplayName),
            new XAttribute("category", definition.Category.ToString()));
        if (definition.DefaultDurationDays is int durationDays) element.SetAttributeValue("defaultDurationDays", BuilderXml.Int(durationDays));
        if (definition.DefaultNonWorkingDay is bool nonWorking) element.SetAttributeValue("defaultNonWorkingDay", BuilderXml.Bool(nonWorking));

        XElement? tags = BuildTagsElement(definition.Tags);
        if (tags is not null) element.Add(tags);

        XElement rules = new(BuilderXml.Namespace + "Rules");
        foreach (NotableDateRuleBuilder rule in definition.Rules)
            rules.Add(BuildRuleElement(rule, requireStrategy: true));

        element.Add(rules);
        return element;
    }

    /// <summary>
    /// Builds a single <c>Rule</c> element, emitting its applicability, strategy, tags, and adjustments in schema
    /// order.
    /// </summary>
    /// <param name="rule">The rule to serialize.</param>
    /// <param name="requireStrategy">A value indicating whether the rule must declare a strategy.</param>
    /// <returns>The serialized rule element.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="requireStrategy" /> is <see langword="true" /> and the rule has no strategy.
    /// </exception>
    private static XElement BuildRuleElement(NotableDateRuleBuilder rule, bool requireStrategy)
    {
        if (requireStrategy && rule.Strategy is null)
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, BuilderResourceStrings.Op_Invalid_RuleStrategyNotSet, rule.Id));

        XElement element = new(BuilderXml.Namespace + "Rule", new XAttribute("id", rule.Id));
        WriteRuleScalars(element, rule);

        if (rule.HasApplicability)
            element.Add(BuildApplicabilityElement(rule));

        if (rule.Strategy is not null)
            element.Add(new XElement(BuilderXml.Namespace + "Strategy", new XElement(rule.Strategy)));

        XElement? tags = BuildTagsElement(rule.Tags);
        if (tags is not null) element.Add(tags);

        XElement? adjustments = BuildRuleAdjustmentsElement(rule.Adjustments);
        if (adjustments is not null) element.Add(adjustments);

        return element;
    }

    /// <summary>
    /// Writes the optional scalar attributes shared by rules and patches onto the supplied element.
    /// </summary>
    /// <param name="element">The element receiving the attributes.</param>
    /// <param name="rule">The rule supplying the scalar values.</param>
    private static void WriteRuleScalars(XElement element, NotableDateRuleBuilder rule)
    {
        if (rule.Priority is int priority) element.SetAttributeValue("priority", BuilderXml.Int(priority));
        if (rule.Category is NotableDateCategory category) element.SetAttributeValue("category", category.ToString());
        if (rule.NonWorking is bool nonWorking) element.SetAttributeValue("nonWorking", BuilderXml.Bool(nonWorking));
        if (rule.DurationDays is int durationDays) element.SetAttributeValue("durationDays", BuilderXml.Int(durationDays));
        if (rule.Comment is not null) element.SetAttributeValue("comment", rule.Comment);
    }

    /// <summary>
    /// Builds an <c>Applicability</c> element from a rule's applicability state.
    /// </summary>
    /// <param name="rule">The rule supplying the applicability values.</param>
    /// <returns>The serialized applicability element.</returns>
    private static XElement BuildApplicabilityElement(NotableDateRuleBuilder rule)
    {
        XElement element = new(BuilderXml.Namespace + "Applicability");
        if (rule.Calendar is CalendarSystem calendar) element.SetAttributeValue("calendar", calendar.ToString());
        if (rule.FromYearValue is int fromYear) element.SetAttributeValue("fromYear", BuilderXml.Int(fromYear));
        if (rule.ToYearValue is int toYear) element.SetAttributeValue("toYear", BuilderXml.Int(toYear));
        if (rule.EveryYearsValue is int everyYears) element.SetAttributeValue("everyYears", BuilderXml.Int(everyYears));
        if (rule.AnchorYearValue is int anchorYear) element.SetAttributeValue("anchorYear", BuilderXml.Int(anchorYear));

        foreach (string territory in rule.Territories)
            element.Add(new XElement(BuilderXml.Namespace + "Territory", new XAttribute("code", territory)));
        foreach (int year in rule.OnlyYearsValues)
            element.Add(new XElement(BuilderXml.Namespace + "OnlyYear", new XAttribute("value", BuilderXml.Int(year))));
        foreach (int year in rule.ExceptYearsValues)
            element.Add(new XElement(BuilderXml.Namespace + "ExceptYear", new XAttribute("value", BuilderXml.Int(year))));

        return element;
    }

    /// <summary>
    /// Builds a <c>Tags</c> element from a tag collection.
    /// </summary>
    /// <param name="tags">The tag values.</param>
    /// <returns>The serialized tags element, or <see langword="null" /> when the collection is empty.</returns>
    private static XElement? BuildTagsElement(IReadOnlyList<string> tags)
    {
        if (tags.Count == 0)
            return null;

        XElement element = new(BuilderXml.Namespace + "Tags");
        foreach (string tag in tags)
            element.Add(new XElement(BuilderXml.Namespace + "Tag", new XAttribute("value", tag)));

        return element;
    }

    /// <summary>
    /// Builds a rule <c>Adjustments</c> element from a collection of policy references.
    /// </summary>
    /// <param name="adjustments">The adjustment policy references.</param>
    /// <returns>The serialized adjustments element, or <see langword="null" /> when the collection is empty.</returns>
    private static XElement? BuildRuleAdjustmentsElement(IReadOnlyList<string> adjustments)
    {
        if (adjustments.Count == 0)
            return null;

        XElement element = new(BuilderXml.Namespace + "Adjustments");
        foreach (string policyRef in adjustments)
            element.Add(new XElement(BuilderXml.Namespace + "Adjustment", new XAttribute("policyRef", policyRef)));

        return element;
    }

    /// <summary>
    /// Builds the <c>Overrides</c> element when at least one override operation is configured.
    /// </summary>
    /// <returns>The overrides element, or <see langword="null" /> when no overrides are configured.</returns>
    /// <exception cref="InvalidOperationException">An added override rule has no strategy.</exception>
    private XElement? BuildOverridesElement()
    {
        if (_overrides is null || _overrides.Entries.Count == 0)
            return null;

        XElement element = new(BuilderXml.Namespace + "Overrides");
        foreach (OverrideEntry entry in _overrides.Entries)
            element.Add(BuildOverrideEntryElement(entry));

        return element;
    }

    /// <summary>
    /// Builds the element for a single override operation.
    /// </summary>
    /// <param name="entry">The override operation to serialize.</param>
    /// <returns>The serialized override element.</returns>
    /// <exception cref="InvalidOperationException">An added override rule has no strategy.</exception>
    private static XElement BuildOverrideEntryElement(OverrideEntry entry) =>
        entry.Operation switch
        {
            OverrideOperation.AddRule => new XElement(
                                BuilderXml.Namespace + "AddRule",
                                new XAttribute("notableDateRef", entry.NotableDateRef),
                                BuildRuleElement(entry.Rule!, requireStrategy: true)),
            OverrideOperation.RemoveRule => new XElement(
                                BuilderXml.Namespace + "RemoveRule",
                                new XAttribute("notableDateRef", entry.NotableDateRef),
                                new XAttribute("ruleRef", entry.RuleRef!)),
            _ => BuildPatchRuleElement(entry),
        };

    /// <summary>
    /// Builds a <c>PatchRule</c> element, emitting only the scalar attributes and rule sections that were configured.
    /// </summary>
    /// <param name="entry">The patch override operation to serialize.</param>
    /// <returns>The serialized patch element.</returns>
    private static XElement BuildPatchRuleElement(OverrideEntry entry)
    {
        NotableDateRuleBuilder rule = entry.Rule!;
        XElement element = new(
            BuilderXml.Namespace + "PatchRule",
            new XAttribute("notableDateRef", entry.NotableDateRef),
            new XAttribute("ruleRef", entry.RuleRef!));
        WriteRuleScalars(element, rule);

        if (rule.HasApplicability)
            element.Add(BuildApplicabilityElement(rule));

        if (rule.Strategy is not null)
            element.Add(new XElement(BuilderXml.Namespace + "Strategy", new XElement(rule.Strategy)));

        XElement? tags = BuildTagsElement(rule.Tags);
        if (tags is not null) element.Add(tags);

        XElement? adjustments = BuildRuleAdjustmentsElement(rule.Adjustments);
        if (adjustments is not null) element.Add(adjustments);

        return element;
    }
}
