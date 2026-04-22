using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar
{
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
		/// Initializes a new instance of the <see cref="ParsedNotableDateDocument" /> record.
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

	/// <summary>
	/// Specifies a single cherry-pick of a <see cref="NotableDateRule" /> from another resource, optionally renaming and overriding
	/// scalar properties on the inherited rule.
	/// </summary>
	/// <param name="SourceRuleName">The name of the rule to pull from the source resource. Matched case-insensitively.</param>
	/// <param name="LocalName">Optional local name. When supplied, the imported rule is exposed under this name instead of <paramref name="SourceRuleName" />.</param>
	/// <param name="Category">Optional category override.</param>
	/// <param name="TerritoryCode">Optional territory override.</param>
	/// <param name="IsNonWorkingDay">Optional non-working day override.</param>
	/// <param name="FirstYear">Optional inclusive first year override.</param>
	/// <param name="LastYear">Optional inclusive last year override.</param>
	/// <param name="OccurrenceYears">Optional recurrence cadence override.</param>
	/// <param name="DurationDays">Optional duration-in-days override.</param>
	/// <param name="Priority">Optional collision priority override.</param>
	/// <param name="Comment">Optional comment override.</param>
	public sealed record NotableDateRuleUseDirective(
		string SourceRuleName,
		string? LocalName = null,
		NotableDateCategory? Category = null,
		string? TerritoryCode = null,
		bool? IsNonWorkingDay = null,
		int? FirstYear = null,
		int? LastYear = null,
		int? OccurrenceYears = null,
		int? DurationDays = null,
		int? Priority = null,
		string? Comment = null);
}
