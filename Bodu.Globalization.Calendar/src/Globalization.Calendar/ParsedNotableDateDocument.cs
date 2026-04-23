// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParsedNotableDateDocument.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;


/// <summary>
/// Represents the contents of a single notable date XML or JSON document, after parsing but before any cherry-pick directives have
/// been resolved.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="ParsedNotableDateDocument" /> is the unit returned by <see cref="NotableDateRuleParser.ParseDocument(string)" />: it
/// captures every directive declared inside a single resource (the <see cref="UseGroups" /> that cherry-pick rules from other
/// resources, and the <see cref="LocalRules" /> that the document declares itself), and lets a higher-level loader such as
/// <see cref="XmlResourceNotableDateRuleProvider" /> flatten a graph of documents into a single rule set under the project's
/// override semantics: locally declared rules win over inherited ones with the same name.
/// </para>
/// </remarks>
public sealed record ParsedNotableDateDocument
{
	/// <summary>
	/// Initialises a new instance of the <see cref="ParsedNotableDateDocument" /> record.
	/// </summary>
	/// <param name="useGroups">The cherry-pick groups that pull rules from other resources.</param>
	/// <param name="localRules">The notable date rules declared locally in this document.</param>
	public ParsedNotableDateDocument(
		ImmutableArray<NotableDateRuleUseGroup> useGroups,
		ImmutableArray<NotableDateRule> localRules)
	{
		UseGroups = useGroups.IsDefault ? ImmutableArray<NotableDateRuleUseGroup>.Empty : useGroups;
		LocalRules = localRules.IsDefault ? ImmutableArray<NotableDateRule>.Empty : localRules;
	}

	/// <summary>
	/// Gets the cherry-pick groups that pull rules from other resources.
	/// </summary>
	public ImmutableArray<NotableDateRuleUseGroup> UseGroups { get; }

	/// <summary>
	/// Gets the notable date rules declared locally in this document.
	/// </summary>
	public ImmutableArray<NotableDateRule> LocalRules { get; }
}
