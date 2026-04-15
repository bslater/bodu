using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar
{
	/// <summary>
	/// Represents the contents of a single notable date XML or JSON document, after parsing but before any imports have been flattened.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A <see cref="ParsedNotableDateDocument" /> is the unit returned by <see cref="NotableDateRuleParser.ParseDocument(string)" />: it
	/// captures every directive declared inside a single resource (the <see cref="Imports" /> it pulls in, the <see cref="Suppressions" />
	/// it removes from inherited content, and the <see cref="Rules" /> it contributes locally), and lets a higher-level loader such as
	/// <see cref="XmlResourceNotableDateRuleProvider" /> flatten a graph of documents into a single rule set under the project's override
	/// semantics: locally declared rules win over imported ones with the same name.
	/// </para>
	/// </remarks>
	public sealed record ParsedNotableDateDocument
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ParsedNotableDateDocument" /> record.
		/// </summary>
		/// <param name="imports">Manifest resource names of documents that this document imports, in declaration order.</param>
		/// <param name="suppressions">Suppression directives that remove inherited rules from the flattened set.</param>
		/// <param name="rules">The notable date rules declared locally in this document.</param>
		public ParsedNotableDateDocument(
			ImmutableArray<string> imports,
			ImmutableArray<NotableDateRuleSuppression> suppressions,
			ImmutableArray<NotableDateRule> rules)
		{
			Imports = imports.IsDefault ? ImmutableArray<string>.Empty : imports;
			Suppressions = suppressions.IsDefault ? ImmutableArray<NotableDateRuleSuppression>.Empty : suppressions;
			Rules = rules.IsDefault ? ImmutableArray<NotableDateRule>.Empty : rules;
		}

		/// <summary>
		/// Gets the manifest resource names that this document imports, in declaration order.
		/// </summary>
		public ImmutableArray<string> Imports { get; }

		/// <summary>
		/// Gets the suppression directives that remove inherited rules from the flattened set.
		/// </summary>
		public ImmutableArray<NotableDateRuleSuppression> Suppressions { get; }

		/// <summary>
		/// Gets the notable date rules declared locally in this document.
		/// </summary>
		public ImmutableArray<NotableDateRule> Rules { get; }
	}

	/// <summary>
	/// Specifies that an inherited <see cref="NotableDateRule" /> should be removed from the flattened rule set.
	/// </summary>
	/// <param name="Name">The name of the rule to suppress. Matched case-insensitively.</param>
	/// <param name="TerritoryCode">
	/// Optional territory scope. When supplied, the suppression only removes the rule when its resolved territory matches; when omitted,
	/// the rule is removed for every territory it would otherwise apply to.
	/// </param>
	public sealed record NotableDateRuleSuppression(string Name, string? TerritoryCode = null);
}
