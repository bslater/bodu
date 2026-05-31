// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleUseGroup.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Cherry-picks one or more <see cref="NotableDateRule" /> instances from a single source resource, optionally
/// selecting them individually with per-rule overrides or pulling the entire source in wholesale.
/// </summary>
/// <remarks>
/// <para>
/// A use-group is the rendering of a single <c>&lt;Use&gt;</c> element in an authored XML or JSON document. It binds a
/// source resource (the document to import from) to a set of <see cref="NotableDateRuleUseDirective" /> entries that
/// say <em>which</em> rules to inherit and <em>how</em> to adapt them. Multiple use-groups in the same document each
/// address a different source resource; together with the document's local rules, they form the
/// <see cref="ParsedNotableDateDocument" /> that the loader flattens before <see cref="NotableDateService" /> resolves
/// it into <see cref="NotableDate" /> values.
/// </para>
/// <para>
/// Two import modes are supported. When <see cref="UseAll" /> is <see langword="true" />, every rule from
/// <see cref="SourceResource" /> is imported; <see cref="Uses" /> may still appear and supply per-rule overrides for
/// specific inherited rules without affecting the rest. When <see cref="UseAll" /> is <see langword="false" />, only
/// the rules referenced by <see cref="Uses" /> are imported — making the directive a true cherry-pick.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Compose a document that imports every public-holiday rule from a global resource and one specific rule from a
/// regional resource, retargeted to a subdivision:
/// </para>
/// <code>
///<![CDATA[
/// Pull every rule from the global public-holidays resource, unchanged:
/// NotableDateRuleUseGroup global = new NotableDateRuleUseGroup(
///     SourceResource: "Bodu/Globalization/Calendar/Resources/global-public.xml",
///     UseAll: true,
///     Uses: ImmutableArray<NotableDateRuleUseDirective>.Empty);
///
/// From the AU public-holidays resource, pull only Anzac Day, retargeted to Western Australia:
/// NotableDateRuleUseGroup australia = new NotableDateRuleUseGroup(
///     SourceResource: "Bodu/Globalization/Calendar/Resources/au-public.xml",
///     UseAll: false,
///     Uses: ImmutableArray.Create(
///         new NotableDateRuleUseDirective(
///             SourceRuleName: "Anzac Day",
///             TerritoryCode: "AU-WA",
///             IsNonWorkingDay: true)));
///
/// Equivalent XML:
/// <Use source="...global-public.xml" useAll="true" />
/// <Use source="...au-public.xml">
///   <Rule sourceName="Anzac Day" territoryCode="AU-WA" isNonWorkingDay="true" />
/// </Use>
///]]>
/// </code>
/// </example>
/// <param name="SourceResource">
/// The manifest resource name of the source document. Must not be <see langword="null" /> or whitespace.
/// </param>
/// <param name="UseAll">
/// When <see langword="true" />, every rule from <paramref name="SourceResource" /> is pulled in;
/// <paramref name="Uses" /> may still supply per-rule overrides. When <see langword="false" />, only the rules listed
/// in <paramref name="Uses" /> are pulled in.
/// </param>
/// <param name="Uses">
/// The explicit per-rule directives. May be empty when <paramref name="UseAll" /> is <see langword="true" />, in which
/// case every rule from the source is inherited unchanged.
/// </param>
public sealed record NotableDateRuleUseGroup(
    string SourceResource,
    bool UseAll,
    ImmutableArray<NotableDateRuleUseDirective> Uses);
