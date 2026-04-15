using System.Reflection;

namespace Bodu.Globalization.Calendar
{
	/// <summary>
	/// Loads a set of <see cref="NotableDateRule" /> instances from an embedded XML resource.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The XML payload is parsed once on first access via the schema-validating <see cref="NotableDateRuleParser" />, after which results
	/// are returned from a thread-safe lazy cache.
	/// </para>
	/// <para>
	/// This class replaces <c>XmlResourceCalendarMetadataSource</c>. The new name aligns with the <see cref="INotableDateRuleProvider" />
	/// vocabulary used elsewhere in the project.
	/// </para>
	/// </remarks>
	public sealed class XmlResourceNotableDateRuleProvider : INotableDateRuleProvider
	{
		private readonly string _xmlResourceName;
		private readonly Assembly _assembly;
		private readonly Lazy<List<NotableDateRule>> _rules;

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlResourceNotableDateRuleProvider" /> class.
		/// </summary>
		/// <param name="xmlResourceName">The full manifest resource name of the embedded XML payload. Must not be <see langword="null" />.</param>
		/// <param name="assembly">The assembly containing the embedded resource. Defaults to the currently executing assembly.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="xmlResourceName" /> is <see langword="null" />.</exception>
		public XmlResourceNotableDateRuleProvider(string xmlResourceName, Assembly? assembly = null)
		{
			_xmlResourceName = xmlResourceName ?? throw new ArgumentNullException(nameof(xmlResourceName));
			_assembly = assembly ?? Assembly.GetExecutingAssembly();
			_rules = new Lazy<List<NotableDateRule>>(LoadAndParseRules, isThreadSafe: true);
		}

		/// <inheritdoc />
		public IEnumerable<NotableDateRule> LoadRules() => _rules.Value;

		/// <summary>
		/// Loads and parses the embedded XML resource into <see cref="NotableDateRule" /> instances.
		/// </summary>
		/// <returns>The parsed rules.</returns>
		/// <exception cref="FileNotFoundException">Thrown when the embedded resource cannot be located.</exception>
		private List<NotableDateRule> LoadAndParseRules()
		{
			using var stream = _assembly.GetManifestResourceStream(_xmlResourceName)
				?? throw new FileNotFoundException($"Embedded XML resource '{_xmlResourceName}' was not found in assembly '{_assembly.FullName}'.");

			using var reader = new StreamReader(stream);
			var xml = reader.ReadToEnd();

			return NotableDateRuleParser.ParseXml(xml);
		}
	}
}
