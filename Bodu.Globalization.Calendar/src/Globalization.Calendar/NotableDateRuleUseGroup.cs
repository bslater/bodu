// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleUseGroup.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;


/// <summary>
/// Cherry-picks one or more <see cref="NotableDateRule" /> instances from a single source resource.
/// </summary>
/// <param name="SourceResource">The manifest resource name of the source document. Must not be <see langword="null" /> or whitespace.</param>
/// <param name="UseAll">
/// When <see langword="true" />, every rule from <paramref name="SourceResource" /> is pulled in; <paramref name="Uses" /> may still
/// supply per-rule overrides. When <see langword="false" />, only the rules listed in <paramref name="Uses" /> are pulled in.
/// </param>
/// <param name="Uses">
/// The explicit per-rule directives. May be empty when <paramref name="UseAll" /> is <see langword="true" />, in which case every
/// rule from the source is inherited unchanged.
/// </param>
public sealed record NotableDateRuleUseGroup(
	string SourceResource,
	bool UseAll,
	ImmutableArray<NotableDateRuleUseDirective> Uses);
