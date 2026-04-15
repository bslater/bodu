using System.Reflection;

namespace Bodu.Globalization.Calendar
{
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

		private List<NotableDateRule> LoadAndFlatten()
		{
			var documentCache = new Dictionary<string, ParsedNotableDateDocument>(StringComparer.OrdinalIgnoreCase);
			var flattenedCache = new Dictionary<string, IReadOnlyDictionary<RuleKey, NotableDateRule>>(StringComparer.OrdinalIgnoreCase);
			var inProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			var byKey = FlattenResource(_rootResourceName, documentCache, flattenedCache, inProgress);
			return byKey.Values.ToList();
		}

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

		private static NotableDateRule? FindSourceRule(IReadOnlyDictionary<RuleKey, NotableDateRule> sourceRules, string name)
		{
			foreach (var pair in sourceRules)
			{
				if (string.Equals(pair.Key.Name, name, StringComparison.OrdinalIgnoreCase))
					return pair.Value;
			}

			return null;
		}

		private static RuleKey KeyOf(NotableDateRule rule) =>
			new(rule.Name, rule.TerritoryCode);

		private static NotableDateRule ApplyOverrides(NotableDateRule source, NotableDateRuleUseDirective directive)
		{
			return source with
			{
				Name = string.IsNullOrWhiteSpace(directive.LocalName) ? source.Name : directive.LocalName!,
				Category = directive.Category ?? source.Category,
				TerritoryCode = directive.TerritoryCode ?? source.TerritoryCode,
				IsNonWorkingDay = directive.IsNonWorkingDay ?? source.IsNonWorkingDay,
				FirstYear = directive.FirstYear ?? source.FirstYear,
				LastYear = directive.LastYear ?? source.LastYear,
				OccurrenceYears = directive.OccurrenceYears ?? source.OccurrenceYears,
				DurationDays = directive.DurationDays ?? source.DurationDays,
				Priority = directive.Priority ?? source.Priority,
				Comment = directive.Comment ?? source.Comment,
			};
		}

		/// <summary>
		/// Compound dedupe key used inside the flatten pipeline. Two rules with the same name but different territories survive as
		/// independent entries so that regional variants (for example, the Scotland-only Summer Bank Holiday alongside the
		/// England/Wales/Northern-Ireland one) are not collapsed.
		/// </summary>
		private readonly record struct RuleKey(string Name, string? Territory)
		{
			public bool Equals(RuleKey other) =>
				string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(Territory ?? string.Empty, other.Territory ?? string.Empty, StringComparison.OrdinalIgnoreCase);

			public override int GetHashCode() =>
				HashCode.Combine(
					StringComparer.OrdinalIgnoreCase.GetHashCode(Name ?? string.Empty),
					StringComparer.OrdinalIgnoreCase.GetHashCode(Territory ?? string.Empty));
		}

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
}
