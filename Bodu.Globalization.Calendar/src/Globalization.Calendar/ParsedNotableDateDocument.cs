// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParsedNotableDateDocument.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents the contents of a single notable date XML or JSON document, after parsing but before any cherry-pick
/// directives have been resolved.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ParsedNotableDateDocument" /> is the unit returned by
/// <see cref="NotableDateRuleParser.ParseDocument(string)" /> and its JSON counterpart
/// <see cref="NotableDateRuleJsonParser" />: it captures every directive declared inside a single resource — the
/// <see cref="UseGroups" /> that cherry-pick rules from sibling resources, and the <see cref="LocalRules" /> that the
/// document declares itself.
/// </para>
/// <para>
/// The parsed document is the input to a higher-level loader such as <see cref="XmlResourceNotableDateRuleProvider" />,
/// which flattens a graph of documents into a single rule set under the project's override semantics: locally declared
/// rules win over inherited ones with the same name, and inherited rules are merged with any
/// <see cref="NotableDateRuleUseDirective.OverrideBody" /> supplied on the importing <c>&lt;Use&gt;</c> directive.
/// </para>
/// <para>
/// At this point the document still references inherited rules by name only — no resolution or merging has happened.
/// The rules emitted by the loader (and ultimately the <see cref="NotableDate" /> values produced by
/// <see cref="NotableDateService" />) only take their final shape after the <see cref="UseGroups" /> have been walked
/// and the resulting <see cref="NotableDateRule" /> instances merged with <see cref="LocalRules" />.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Parse a small XML payload and inspect what each collection contains:
/// </para>
/// <code>
///<![CDATA[
/// const string xml = """
///     &lt;NotableDates xmlns="urn:bodu:globalization:calendar"&gt;
///       &lt;Use source="Bodu/Globalization/Calendar/Resources/global-public.xml"&gt;
///         &lt;Rule sourceName="New Year's Day" territoryCode="AU" /&gt;
///       &lt;/Use&gt;
///       &lt;Rule name="Australia Day" category="Public" territoryCode="AU" isNonWorkingDay="true"&gt;
///         &lt;Fixed month="1" day="26" /&gt;
///       &lt;/Rule&gt;
///     &lt;/NotableDates&gt;
///     """;
///
/// ParsedNotableDateDocument document = NotableDateRuleParser.ParseDocument(xml);
///
/// // document.UseGroups[0].SourceResource == ".../global-public.xml"
/// // document.UseGroups[0].Uses[0].SourceRuleName == "New Year's Day"
/// // document.LocalRules[0].Name == "Australia Day"
///]]>
/// </code>
/// </example>
public sealed record ParsedNotableDateDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParsedNotableDateDocument" /> class.
    /// </summary>
    /// <param name="useGroups">
    /// The cherry-pick groups that pull rules from other resources. Default-uninitialized arrays are coerced to
    /// <see cref="ImmutableArray{T}.Empty" />.
    /// </param>
    /// <param name="localRules">
    /// The notable date rules declared locally in this document. Default-uninitialized arrays are coerced to
    /// <see cref="ImmutableArray{T}.Empty" />.
    /// </param>
    public ParsedNotableDateDocument(
        ImmutableArray<NotableDateRuleUseGroup> useGroups,
        ImmutableArray<NotableDateRule> localRules)
    {
        UseGroups = useGroups.IsDefault ? [] : useGroups;
        LocalRules = localRules.IsDefault ? [] : localRules;
    }

    /// <summary>
    /// Gets the cherry-pick groups that pull rules from other resources, in document order.
    /// </summary>
    /// <returns>
    /// The <see cref="NotableDateRuleUseGroup" /> entries authored on the document, or
    /// <see cref="ImmutableArray{T}.Empty" /> when no <c>&lt;Use&gt;</c> directives were declared.
    /// </returns>
    public ImmutableArray<NotableDateRuleUseGroup> UseGroups { get; }

    /// <summary>
    /// Gets the notable date rules declared locally in this document, in document order.
    /// </summary>
    /// <returns>
    /// The <see cref="NotableDateRule" /> entries authored directly on the document, or
    /// <see cref="ImmutableArray{T}.Empty" /> when no local rules were declared.
    /// </returns>
    public ImmutableArray<NotableDateRule> LocalRules { get; }
}
