using System.Reflection;

namespace Bodu.Globalization.Calendar
{
	/// <summary>
	/// Loads a graph of <see cref="NotableDateRule" /> instances from an embedded XML resource, recursively resolving any
	/// <c>&lt;Import&gt;</c> directives and applying any <c>&lt;Suppress&gt;</c> directives.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The provider implements the project's import/override semantics:
	/// </para>
	/// <list type="number">
	/// <item><description>The supplied root resource is parsed.</description></item>
	/// <item><description>Each <c>&lt;Import&gt;</c> directive is resolved recursively, depth-first, with cycle detection.</description></item>
	/// <item><description>The flattened rule set is built by walking the import order from deepest to shallowest. When two rules share a name, the rule defined latest (typically in the importing document) replaces the earlier one.</description></item>
	/// <item><description>Suppression directives remove inherited rules from the flattened set. A suppression with no <see cref="NotableDateRuleSuppression.TerritoryCode" /> removes every variant of the named rule; a territory-scoped suppression removes only matching variants.</description></item>
	/// </list>
	/// <para>
	/// The flattened result is computed once on first access and cached behind a thread-safe <see cref="Lazy{T}" />.
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
			var loaded = new Dictionary<string, ParsedNotableDateDocument>(StringComparer.OrdinalIgnoreCase);
			var inProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			LoadDocumentRecursive(_rootResourceName, loaded, inProgress);

			// Walk the import graph depth-first, so that each document sees only the rules contributed by its descendants when it
			// applies its own suppressions and additions. This ensures that suppressions remove inherited content but never the
			// suppressing document's own local rules.
			var byName = new Dictionary<string, NotableDateRule>(StringComparer.OrdinalIgnoreCase);
			ApplyDocument(_rootResourceName, loaded, byName, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

			return byName.Values.ToList();
		}

		private void LoadDocumentRecursive(
			string resourceName,
			Dictionary<string, ParsedNotableDateDocument> loaded,
			HashSet<string> inProgress)
		{
			if (loaded.ContainsKey(resourceName))
				return;

			if (!inProgress.Add(resourceName))
				throw new InvalidOperationException($"Circular import detected while loading '{resourceName}'.");

			try
			{
				var document = LoadAndParse(resourceName);
				loaded[resourceName] = document;

				foreach (var import in document.Imports)
				{
					LoadDocumentRecursive(import, loaded, inProgress);
				}
			}
			finally
			{
				inProgress.Remove(resourceName);
			}
		}

		private static void ApplyDocument(
			string resourceName,
			IReadOnlyDictionary<string, ParsedNotableDateDocument> loaded,
			Dictionary<string, NotableDateRule> byName,
			HashSet<string> applied)
		{
			if (!applied.Add(resourceName))
				return;

			var document = loaded[resourceName];

			// 1. Imported documents contribute first so the importing document can override them by name.
			foreach (var import in document.Imports)
			{
				ApplyDocument(import, loaded, byName, applied);
			}

			// 2. The importing document's suppressions remove inherited rules now, before its own rules are added. This guarantees that a
			//    document never accidentally suppresses its own local rules.
			foreach (var suppression in document.Suppressions)
			{
				ApplySuppression(byName, suppression);
			}

			// 3. Local rules are added (or replace any that survived suppression). Locally-declared rules always win over inherited ones.
			foreach (var rule in document.Rules)
			{
				if (string.IsNullOrWhiteSpace(rule.Name))
					continue;

				byName[rule.Name] = rule;
			}
		}

		private static void ApplySuppression(Dictionary<string, NotableDateRule> byName, NotableDateRuleSuppression suppression)
		{
			if (!byName.TryGetValue(suppression.Name, out var existing))
				return;

			if (string.IsNullOrEmpty(suppression.TerritoryCode))
			{
				byName.Remove(suppression.Name);
				return;
			}

			// Territory-scoped suppression: remove only when the rule's territory contains the suppression scope.
			if (string.IsNullOrEmpty(existing.TerritoryCode))
				return;

			if (!TerritoryCode.TryParse(suppression.TerritoryCode, out var suppressionScope))
				return;

			foreach (var territory in TerritoryCode.ParseList(existing.TerritoryCode))
			{
				if (suppressionScope.Contains(territory))
				{
					byName.Remove(suppression.Name);
					return;
				}
			}
		}

		private ParsedNotableDateDocument LoadAndParse(string resourceName)
		{
			using var stream = _assembly.GetManifestResourceStream(resourceName)
				?? throw new FileNotFoundException(
					$"Embedded XML resource '{resourceName}' was not found in assembly '{_assembly.FullName}'.");

			using var reader = new StreamReader(stream);
			var xml = reader.ReadToEnd();
			return NotableDateRuleParser.ParseDocument(xml);
		}
	}
}
