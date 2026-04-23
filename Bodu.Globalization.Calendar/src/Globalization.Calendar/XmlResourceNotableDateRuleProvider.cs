// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlResourceNotableDateRuleProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Loads a graph of <see cref="NotableDateRule" /> instances from an embedded XML resource, recursively resolving every cherry-pick
/// directive declared via <c>&lt;UseFrom&gt;</c> / <c>&lt;Use&gt;</c> / <c>&lt;UseAll&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// The provider implements the project's explicit cherry-pick semantics:
/// </para>
/// <list type="number">
/// <item><description>The supplied root resource is parsed.</description></item>
/// <item><description>Each referenced source resource is loaded and recursively flattened, with cycle detection.</description></item>
/// <item><description>For every <c>&lt;UseFrom&gt;</c> directive, the provider pulls the named rules (or every rule, when <c>&lt;UseAll&gt;</c> is present) from the source's flattened set, applies any per-directive scalar overrides, and adds the resulting rules to the local set.</description></item>
/// <item><description>Locally declared <c>&lt;NotableDate&gt;</c> entries are added last and override any inherited rules with the same name.</description></item>
/// </list>
/// <para>
/// Adding a new rule to a source resource never cascades into its consumers: every consumer must explicitly list the rule in a
/// <c>&lt;Use&gt;</c> directive (or opt in via <c>&lt;UseAll /&gt;</c>) for that rule to appear in the consumer's flattened set.
/// </para>
/// </remarks>
public sealed class XmlResourceNotableDateRuleProvider : INotableDateRuleProvider
{
	private readonly string _rootResourceName;
	private readonly Assembly _assembly;
	private readonly Lazy<List<NotableDateRule>> _flattenedRules;

	/// <summary>
	/// Initializes a new instance of the <see cref="XmlResourceNotableDateRuleProvider" /> class.
	/// </summary>
	/// <param name="xmlResourceName">The full manifest resource name of the root XML payload. Must not be <see langword="null" />.</param>
	/// <param name="assembly">The assembly containing the embedded resource(s). Defaults to the currently executing assembly.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="xmlResourceName" /> is <see langword="null" />.</exception>
	public XmlResourceNotableDateRuleProvider(string xmlResourceName, Assembly? assembly = null)
	{
		_rootResourceName = xmlResourceName ?? throw new ArgumentNullException(nameof(xmlResourceName));
		_assembly = assembly ?? Assembly.GetExecutingAssembly();
		_flattenedRules = new Lazy<List<NotableDateRule>>(LoadAndFlatten, isThreadSafe: true);
	}

	/// <inheritdoc />
	public IEnumerable<NotableDateRule> LoadRules() => _flattenedRules.Value;

	// ----------------------------------------------------------------------------
	// Loading and flattening
	// ----------------------------------------------------------------------------

    /// <summary>
    /// Loads every configured XML resource and flattens their rule definitions — including
    /// cross-file &lt;UseFrom&gt; references — into a single rule list.
    /// </summary>
    /// <returns>The materialised rule list.</returns>
	private List<NotableDateRule> LoadAndFlatten()
	{
		var documentCache = new Dictionary<string, ParsedNotableDateDocument>(StringComparer.OrdinalIgnoreCase);
		var flattenedCache = new Dictionary<string, IReadOnlyDictionary<RuleKey, NotableDateRule>>(StringComparer.OrdinalIgnoreCase);
		var inProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		var byKey = FlattenResource(_rootResourceName, documentCache, flattenedCache, inProgress);
		return byKey.Values.ToList();
	}

    /// <summary>
    /// Flattens a single parsed resource document into a rule dictionary keyed by
    /// <see cref="RuleKey" />, resolving &lt;Use&gt; directives against already-loaded sources.
    /// </summary>
	private IReadOnlyDictionary<RuleKey, NotableDateRule> FlattenResource(
		string resourceName,
		Dictionary<string, ParsedNotableDateDocument> documentCache,
		Dictionary<string, IReadOnlyDictionary<RuleKey, NotableDateRule>> flattenedCache,
		HashSet<string> inProgress)
	{
		if (flattenedCache.TryGetValue(resourceName, out var cachedFlattened))
			return cachedFlattened;

		if (!inProgress.Add(resourceName))
			throw new InvalidOperationException(
				$"Circular reference detected while flattening notable date resource '{resourceName}'.");

		try
		{
			var document = LoadDocument(resourceName, documentCache);
			var byKey = new Dictionary<RuleKey, NotableDateRule>();

			foreach (var group in document.UseGroups)
			{
				var sourceRules = FlattenResource(group.SourceResource, documentCache, flattenedCache, inProgress);

				if (group.UseAll)
				{
					// Wildcard: copy every rule. Explicit Use directives below may then override individual entries.
					foreach (var pair in sourceRules)
					{
						byKey[pair.Key] = pair.Value;
					}
				}

				foreach (var directive in group.Uses)
				{
					// Source lookup is by name only; if multiple rules share the same name in the source (e.g. regional variants
					// of "Labour Day"), the first match is used. Authors who need finer-grained selection should rename the
					// source rule or scope it to a unique territory.
					var sourceRule = FindSourceRule(sourceRules, directive.SourceRuleName)
						?? throw new InvalidOperationException(
							$"Notable date rule '{directive.SourceRuleName}' was not found in source resource '{group.SourceResource}' (referenced from '{resourceName}').");

					var localised = ApplyOverrides(sourceRule, directive);
					byKey[KeyOf(localised)] = localised;
				}
			}

			// Locally-declared rules always win over inherited ones with the same (name, territory) key.
			foreach (var rule in document.LocalRules)
			{
				if (string.IsNullOrWhiteSpace(rule.Name))
					continue;

				byKey[KeyOf(rule)] = rule;
			}

			flattenedCache[resourceName] = byKey;
			return byKey;
		}
		finally
		{
			inProgress.Remove(resourceName);
		}
	}

    /// <summary>
    /// Searches <paramref name="sourceRules" /> for a rule that satisfies the &lt;Use&gt;
    /// directive's name and optional territory scope.
    /// </summary>
    /// <param name="sourceRules">The already-loaded rule dictionary.</param>
    /// <param name="name">The rule name from the &lt;Use&gt; directive.</param>
    /// <returns>The matching rule, or <see langword="null" /> if no rule with the given name is
    /// present in <paramref name="sourceRules" />.</returns>
	private static NotableDateRule? FindSourceRule(IReadOnlyDictionary<RuleKey, NotableDateRule> sourceRules, string name)
	{
		foreach (var pair in sourceRules)
		{
			if (string.Equals(pair.Key.Name, name, StringComparison.OrdinalIgnoreCase))
				return pair.Value;
		}

		return null;
	}

    /// <summary>
    /// Builds the composite <see cref="RuleKey" /> (name + territory) used to deduplicate rules
    /// within a single resource.
    /// </summary>
    /// <param name="rule">The rule to key.</param>
    /// <returns>The composite key.</returns>
	private static RuleKey KeyOf(NotableDateRule rule) =>
		new(rule.Name, rule.TerritoryCode);

    /// <summary>
    /// Returns a copy of <paramref name="source" /> with every override from <paramref name="directive" /> applied via
    /// <see cref="NotableDateRuleMerger.Apply" />. The merge algorithm lives in a dedicated helper so it can be exercised in
    /// isolation without bootstrapping an assembly loader.
    /// </summary>
    /// <param name="source">The base rule being re-used.</param>
    /// <param name="directive">The &lt;Use&gt; directive specifying overrides.</param>
    /// <returns>The overridden rule.</returns>
	private static NotableDateRule ApplyOverrides(NotableDateRule source, NotableDateRuleUseDirective directive) =>
		NotableDateRuleMerger.Apply(source, directive);

	/// <summary>
	/// Compound dedupe key used inside the flatten pipeline. Two rules with the same name but different territories survive as
	/// independent entries so that regional variants (for example, the Scotland-only Summer Bank Holiday alongside the
	/// England/Wales/Northern-Ireland one) are not collapsed.
	/// </summary>
	private readonly record struct RuleKey(string Name, string? Territory)
	{
        /// <summary>
        /// Returns <see langword="true" /> if <paramref name="other" /> has the same name and
        /// territory as this key.
        /// </summary>
        /// <param name="other">The key to compare against.</param>
        /// <returns><see langword="true" /> if equal; otherwise <see langword="false" />.</returns>
		public bool Equals(RuleKey other) =>
			string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(Territory ?? string.Empty, other.Territory ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Returns the hash code combining the rule name and territory.
        /// </summary>
        /// <returns>The composite hash code.</returns>
		public override int GetHashCode() =>
			HashCode.Combine(
				StringComparer.OrdinalIgnoreCase.GetHashCode(Name ?? string.Empty),
				StringComparer.OrdinalIgnoreCase.GetHashCode(Territory ?? string.Empty));
	}

    /// <summary>
    /// Loads a single embedded XML resource, schema-validates it, and parses it into a
    /// <see cref="ParsedNotableDateDocument" />.
    /// </summary>
    /// <param name="resourceName">The fully-qualified embedded resource name.</param>
    /// <param name="documentCache">A per-call cache used to deduplicate resource loads when
    /// multiple &lt;Use&gt; directives reference the same source.</param>
    /// <returns>The parsed document model.</returns>
	private ParsedNotableDateDocument LoadDocument(
		string resourceName,
		Dictionary<string, ParsedNotableDateDocument> documentCache)
	{
		if (documentCache.TryGetValue(resourceName, out var cached))
			return cached;

		using var stream = _assembly.GetManifestResourceStream(resourceName)
			?? throw new FileNotFoundException(
				$"Embedded XML resource '{resourceName}' was not found in assembly '{_assembly.FullName}'.");

		using var reader = new StreamReader(stream);
		var xml = reader.ReadToEnd();
		var document = NotableDateRuleParser.ParseDocument(xml);

		documentCache[resourceName] = document;
		return document;
	}
}
