// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleResourceProviderBase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the shared embedded-resource flattening pipeline used by format-specific
/// <see cref="INotableDateRuleProvider" /> implementations such as
/// <see cref="XmlResourceNotableDateRuleProvider" /> and <see cref="JsonResourceNotableDateRuleProvider" />.
/// </summary>
/// <remarks>
/// <para>
/// The base class owns the entire cherry-pick resolution algorithm: it loads the configured root resource,
/// recursively walks every <c>UseFrom</c> directive, applies per-directive overrides via
/// <see cref="NotableDateRuleMerger" />, and produces a single flattened rule list that locally declared rules
/// can override.
/// </para>
/// <para>
/// Resource lookup walks the configured assembly chain in order: the first assembly containing a requested
/// resource wins. This lets a companion data pack be layered over the main library so that a
/// <c>UseFrom</c> directive can resolve targets that live in a different assembly.
/// </para>
/// <para>
/// Document parsing dispatches by file extension: resources whose names end with <c>.json</c>
/// (case-insensitive) are parsed by <see cref="NotableDateRuleJsonParser" />; everything else falls back to
/// <see cref="NotableDateRuleParser" />. This makes mixed-format graphs transparent — a JSON document may
/// <c>useFrom</c> an XML resource and vice versa, with no configuration required.
/// </para>
/// </remarks>
public abstract class NotableDateRuleResourceProviderBase : INotableDateRuleProvider
{
	/// <summary>The logical path of the root resource file that seeds the flatten pipeline.</summary>
	private readonly string _rootResourceName;

	/// <summary>The path resolver used to translate relative <c>UseFrom</c> paths into fully qualified resource names.</summary>
	private readonly IResourcePathResolver _resourcePathResolver;

	/// <summary>The ordered list of assemblies searched for embedded manifest resources during flattening; the first assembly containing a requested resource wins.</summary>
	private readonly IReadOnlyList<Assembly> _assemblies;

	/// <summary>Thread-safe lazy backing store for the fully flattened rule list; populated on first call to <see cref="LoadRules" />.</summary>
	private readonly Lazy<List<NotableDateRule>> _flattenedRules;

	/// <summary>
	/// Initialises a new instance of the <see cref="NotableDateRuleResourceProviderBase" /> class that resolves
	/// embedded resources against a single assembly.
	/// </summary>
	/// <param name="rootResourceName">The logical resource path of the root payload (for example
	/// <c>Bodu/Globalization/Calendar/Resources/global-all.xml</c>). Must not be <see langword="null" />.</param>
	/// <param name="resourcePathResolver">The resolver used to translate relative <c>UseFrom</c> paths into
	/// fully qualified resource names. Must not be <see langword="null" />.</param>
	/// <param name="assembly">The assembly containing the embedded resource(s). Defaults to the currently
	/// executing assembly when <see langword="null" />.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="rootResourceName" /> or
	/// <paramref name="resourcePathResolver" /> is <see langword="null" />.</exception>
	protected NotableDateRuleResourceProviderBase(
		string rootResourceName,
		IResourcePathResolver resourcePathResolver,
		Assembly? assembly)
		: this(rootResourceName, resourcePathResolver, new[] { assembly ?? Assembly.GetExecutingAssembly() })
	{
	}

	/// <summary>
	/// Initialises a new instance of the <see cref="NotableDateRuleResourceProviderBase" /> class that resolves
	/// embedded resources against an ordered chain of assemblies.
	/// </summary>
	/// <param name="rootResourceName">The logical resource path of the root payload. Must not be
	/// <see langword="null" />.</param>
	/// <param name="resourcePathResolver">The resolver used to translate relative <c>UseFrom</c> paths into
	/// fully qualified resource names. Must not be <see langword="null" />.</param>
	/// <param name="assemblies">The ordered chain of assemblies searched for embedded resources; the first
	/// assembly containing a requested resource wins. Use this overload to layer a companion data pack over
	/// the main library, so <c>UseFrom</c> directives can resolve targets that live in a different assembly.
	/// Must not be <see langword="null" /> or empty, and must not contain <see langword="null" /> entries.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="rootResourceName" />,
	/// <paramref name="resourcePathResolver" />, or <paramref name="assemblies" /> is
	/// <see langword="null" />.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="assemblies" /> is empty or contains a
	/// <see langword="null" /> entry.</exception>
	protected NotableDateRuleResourceProviderBase(
		string rootResourceName,
		IResourcePathResolver resourcePathResolver,
		IEnumerable<Assembly> assemblies)
	{
		_rootResourceName = rootResourceName ?? throw new ArgumentNullException(nameof(rootResourceName));
		_resourcePathResolver = resourcePathResolver ?? throw new ArgumentNullException(nameof(resourcePathResolver));
		if (assemblies is null) throw new ArgumentNullException(nameof(assemblies));

		Assembly[] snapshot = assemblies.ToArray();
		if (snapshot.Length == 0) throw new ArgumentException("At least one assembly must be supplied.", nameof(assemblies));
		for (int i = 0; i < snapshot.Length; i++)
		{
			if (snapshot[i] is null) throw new ArgumentException("Assembly chain entries must not be null.", nameof(assemblies));
		}

		_assemblies = snapshot;
		_flattenedRules = new Lazy<List<NotableDateRule>>(LoadAndFlatten, isThreadSafe: true);
	}

	/// <inheritdoc />
	public IEnumerable<NotableDateRule> LoadRules() => _flattenedRules.Value;

	// ----------------------------------------------------------------------------
	// Loading and flattening
	// ----------------------------------------------------------------------------

	/// <summary>
	/// Loads every configured resource and flattens the rule definitions — including cross-file
	/// <c>UseFrom</c> references — into a single rule list.
	/// </summary>
	/// <returns>The materialised rule list.</returns>
	private List<NotableDateRule> LoadAndFlatten()
	{
		var documentCache = new Dictionary<string, ParsedNotableDateDocument>(StringComparer.OrdinalIgnoreCase);
		var flattenedCache = new Dictionary<string, IReadOnlyDictionary<RuleKey, NotableDateRule>>(StringComparer.OrdinalIgnoreCase);
		var inProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		var byKey = FlattenResource(_rootResourceName, _resourcePathResolver, documentCache, flattenedCache, inProgress);
		return byKey.Values.ToList();
	}

	/// <summary>
	/// Flattens a single parsed resource document into a rule dictionary keyed by <see cref="RuleKey" />,
	/// resolving <c>Use</c> directives against already-loaded sources.
	/// </summary>
	private IReadOnlyDictionary<RuleKey, NotableDateRule> FlattenResource(
		string resourceName,
		IResourcePathResolver pathResolver,
		Dictionary<string, ParsedNotableDateDocument> documentCache,
		Dictionary<string, IReadOnlyDictionary<RuleKey, NotableDateRule>> flattenedCache,
		HashSet<string> inProgress)
	{
		if (flattenedCache.TryGetValue(resourceName, out var cachedFlattened))
			return cachedFlattened;

		if (!inProgress.Add(resourceName))
			throw new InvalidOperationException(
				string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_CircularReferenceInResource, resourceName));

		try
		{
			var document = LoadDocument(resourceName, documentCache);
			var byKey = new Dictionary<RuleKey, NotableDateRule>();

			foreach (var group in document.UseGroups)
			{
				var resolvedPath = pathResolver.Resolve(resourceName, group.SourceResource);
				var sourceRules = FlattenResource(resolvedPath, pathResolver, documentCache, flattenedCache, inProgress);

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
					// Source lookup is by canonical name. A single notable date may be expressed as more than one rule (era splits,
					// regional variants); every match is brought across so the consumer sees the same shape as the source. The
					// override body, if any, is applied either to the rule whose RuleName matches the body's RuleName, or — when
					// the body's RuleName is omitted — to every match. With ClearInherited, the body alone defines the result.
					var matches = FindSourceRules(sourceRules, directive.SourceRuleName);
					if (matches.Count == 0)
					{
						throw new InvalidOperationException(
							string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.InvalidOperationException_RuleNotFoundInSource, directive.SourceRuleName, group.SourceResource, resourceName));
					}

					if (directive.ClearInherited)
					{
						// Drop every inherited match previously copied via UseAll, then promote a single rule built solely from the
						// directive (the override body is applied on top of the first match purely to seed strategy/category/etc.).
						foreach (var match in matches)
						{
							byKey.Remove(KeyOf(match));
						}

						var seed = matches[0];
						var localised = ApplyOverrides(seed, directive);
						byKey[KeyOf(localised)] = localised;
						continue;
					}

					var targetRuleName = directive.OverrideBody?.RuleName;
					var directiveScalarsOnly = directive with { OverrideBody = null };
					foreach (var sourceRule in matches)
					{
						// The override body applies to: a single match unconditionally; every match when no RuleName is supplied;
						// or only the rule whose RuleName matches when the body explicitly identifies one. Other matches receive
						// the directive's flat scalars (territory, nonWorking, etc.) without the body's per-rule overrides.
						bool isOverrideTarget = matches.Count == 1
							|| string.IsNullOrWhiteSpace(targetRuleName)
							|| string.Equals(sourceRule.RuleName, targetRuleName, StringComparison.OrdinalIgnoreCase);

						var localised = ApplyOverrides(sourceRule, isOverrideTarget ? directive : directiveScalarsOnly);

						// Remove the inherited entry under its source-side key before re-keying, since the merge may shift any
						// component of the dedupe key (territory rename via flat attribute, RuleName rename via the body).
						byKey.Remove(KeyOf(sourceRule));
						byKey[KeyOf(localised)] = localised;
					}
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
	/// Converts a logical slash-delimited path into the dot-delimited manifest resource name used by
	/// <see cref="System.Reflection.Assembly.GetManifestResourceStream(string)" />.
	/// </summary>
	/// <param name="logicalPath">The logical resource path to convert. Must not be <see langword="null" />, empty, or whitespace.</param>
	/// <returns>The manifest resource name with slashes replaced by dots and leading/trailing separators stripped.</returns>
	private static string ToProviderPath(string logicalPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(logicalPath);

		return logicalPath
			.Trim()
			.Trim('/')
			.Replace('/', '.');
	}

	/// <summary>
	/// Returns every rule in <paramref name="sourceRules" /> whose canonical
	/// <see cref="NotableDateRule.Name" /> matches <paramref name="name" /> (case-insensitive),
	/// preserving dictionary enumeration order.
	/// </summary>
	/// <param name="sourceRules">The already-loaded rule dictionary.</param>
	/// <param name="name">The canonical rule name from the <c>Use</c> directive.</param>
	/// <returns>The matching rules; empty if none are present.</returns>
	private static List<NotableDateRule> FindSourceRules(IReadOnlyDictionary<RuleKey, NotableDateRule> sourceRules, string name)
	{
		var matches = new List<NotableDateRule>();
		foreach (var pair in sourceRules)
		{
			if (string.Equals(pair.Key.Name, name, StringComparison.OrdinalIgnoreCase))
				matches.Add(pair.Value);
		}

		return matches;
	}

	/// <summary>
	/// Builds the composite <see cref="RuleKey" /> (name + territory + rule name) used to deduplicate rules within a single
	/// resource. Including <see cref="NotableDateRule.RuleName" /> lets a single notable date carry more than one rule
	/// (for example, an era-split <c>King's Birthday</c>) without collapsing variants under the same canonical name and
	/// territory.
	/// </summary>
	/// <param name="rule">The rule to key.</param>
	/// <returns>The composite key.</returns>
	private static RuleKey KeyOf(NotableDateRule rule) =>
		new(rule.Name, rule.TerritoryCode, rule.RuleName);

	/// <summary>
	/// Returns a copy of <paramref name="source" /> with every override from <paramref name="directive" /> applied via
	/// <see cref="NotableDateRuleMerger.Apply" />. The merge algorithm lives in a dedicated helper so it can be exercised in
	/// isolation without bootstrapping an assembly loader.
	/// </summary>
	/// <param name="source">The base rule being re-used.</param>
	/// <param name="directive">The <c>Use</c> directive specifying overrides.</param>
	/// <returns>The overridden rule.</returns>
	private static NotableDateRule ApplyOverrides(NotableDateRule source, NotableDateRuleUseDirective directive) =>
		NotableDateRuleMerger.Apply(source, directive);

	/// <summary>
	/// Compound dedupe key used inside the flatten pipeline. Rules survive as independent entries when any of the three
	/// components differ — so regional variants (e.g. the Scotland-only Summer Bank Holiday alongside the
	/// England/Wales/Northern-Ireland one) and era splits (e.g. Queensland's June and October King's Birthday) are not
	/// collapsed.
	/// </summary>
	private readonly record struct RuleKey(string Name, string? Territory, string? RuleName)
	{
		/// <summary>
		/// Returns <see langword="true" /> if <paramref name="other" /> has the same canonical name, territory, and
		/// rule-level identifier as this key.
		/// </summary>
		/// <param name="other">The key to compare against.</param>
		/// <returns><see langword="true" /> if equal; otherwise <see langword="false" />.</returns>
		public bool Equals(RuleKey other) =>
			string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(Territory ?? string.Empty, other.Territory ?? string.Empty, StringComparison.OrdinalIgnoreCase)
			&& string.Equals(RuleName ?? string.Empty, other.RuleName ?? string.Empty, StringComparison.OrdinalIgnoreCase);

		/// <summary>
		/// Returns the hash code combining the rule name, territory, and rule-level identifier.
		/// </summary>
		/// <returns>The composite hash code.</returns>
		public override int GetHashCode() =>
			HashCode.Combine(
				StringComparer.OrdinalIgnoreCase.GetHashCode(Name ?? string.Empty),
				StringComparer.OrdinalIgnoreCase.GetHashCode(Territory ?? string.Empty),
				StringComparer.OrdinalIgnoreCase.GetHashCode(RuleName ?? string.Empty));
	}

	/// <summary>
	/// Loads a single embedded resource, dispatches to the format-specific parser, and returns the parsed
	/// <see cref="ParsedNotableDateDocument" />. Resources whose names end with <c>.json</c> (case-insensitive) are parsed
	/// by <see cref="NotableDateRuleJsonParser" />; everything else is parsed by <see cref="NotableDateRuleParser" />.
	/// </summary>
	/// <param name="resourceName">The fully-qualified embedded resource name.</param>
	/// <param name="documentCache">A per-call cache used to deduplicate resource loads when multiple <c>Use</c>
	/// directives reference the same source.</param>
	/// <returns>The parsed document model.</returns>
	private ParsedNotableDateDocument LoadDocument(
		string resourceName,
		Dictionary<string, ParsedNotableDateDocument> documentCache)
	{
		resourceName = ToProviderPath(resourceName);

		if (documentCache.TryGetValue(resourceName, out var cached))
			return cached;

		using var stream = OpenManifestResourceStream(resourceName)
			?? throw new FileNotFoundException(
				string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.FileNotFoundException_EmbeddedXmlResourceNotFound, resourceName, FormatAssemblyChain(_assemblies)));

		using var reader = new StreamReader(stream);
		var content = reader.ReadToEnd();

		ParsedNotableDateDocument document = IsJsonResource(resourceName)
			? NotableDateRuleJsonParser.ParseDocument(content)
			: NotableDateRuleParser.ParseDocument(content);

		documentCache[resourceName] = document;
		return document;
	}

	/// <summary>
	/// Walks the configured assembly chain in order and returns the first manifest stream found for the supplied resource name.
	/// </summary>
	/// <param name="resourceName">The fully-qualified manifest resource name.</param>
	/// <returns>The opened stream, or <see langword="null" /> when no assembly in the chain contains the resource.</returns>
	private Stream? OpenManifestResourceStream(string resourceName)
	{
		for (int i = 0; i < _assemblies.Count; i++)
		{
			Stream? stream = _assemblies[i].GetManifestResourceStream(resourceName);
			if (stream is not null)
				return stream;
		}

		return null;
	}

	/// <summary>
	/// Formats the configured assembly chain as a comma-separated list of full names for inclusion in diagnostic messages.
	/// </summary>
	/// <param name="assemblies">The assembly chain to format.</param>
	/// <returns>A human-readable, comma-separated list of assembly full names.</returns>
	private static string FormatAssemblyChain(IReadOnlyList<Assembly> assemblies)
	{
		if (assemblies.Count == 1)
			return assemblies[0].FullName ?? string.Empty;

		return string.Join(", ", assemblies.Select(a => a.FullName ?? string.Empty));
	}

	/// <summary>
	/// Determines whether <paramref name="resourceName" /> identifies a JSON resource based on its file extension.
	/// </summary>
	/// <param name="resourceName">The fully-qualified manifest resource name to inspect.</param>
	/// <returns><see langword="true" /> when the resource name ends with <c>.json</c> (case-insensitive); otherwise
	/// <see langword="false" />.</returns>
	private static bool IsJsonResource(string resourceName) =>
		resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
}
