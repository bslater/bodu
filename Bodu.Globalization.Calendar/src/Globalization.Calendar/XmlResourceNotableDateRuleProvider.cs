// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlResourceNotableDateRuleProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Loads a graph of <see cref="NotableDateRule" /> instances from an embedded XML resource, recursively resolving every
/// cherry-pick directive declared via <c>&lt;UseFrom&gt;</c> / <c>&lt;Use&gt;</c> / <c>&lt;UseAll&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// The provider implements the project's explicit cherry-pick semantics:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// The supplied root resource is parsed.
/// </description>
/// </item>
/// <item>
/// <description>
/// Each referenced source resource is loaded and recursively flattened, with cycle detection.
/// </description>
/// </item>
/// <item>
/// <description>
/// For every <c>&lt;UseFrom&gt;</c> directive, the provider pulls the named rules (or every rule when
/// <c>&lt;UseAll&gt;</c> is present) from the source's flattened set, applies any per-directive scalar overrides, and
/// adds the resulting rules to the local set.
/// </description>
/// </item>
/// <item>
/// <description>
/// Locally declared <c>&lt;NotableDate&gt;</c> entries are added last and override any inherited rules with the same
/// name.
/// </description>
/// </item>
/// </list>
/// <para>
/// Adding a new rule to a source resource never cascades into its consumers: every consumer must explicitly list the
/// rule in a <c>&lt;Use&gt;</c> directive (or opt in via <c>&lt;UseAll /&gt;</c>) for that rule to appear in the
/// consumer's flattened set.
/// </para>
/// <para>
/// All format-agnostic flattening logic lives in <see cref="NotableDateRuleResourceProviderBase" />. This class is a
/// thin alias preserved for discoverability and binary compatibility with the original XML-only API; the base class
/// transparently dispatches to <see cref="NotableDateRuleJsonParser" /> when a referenced <c>&lt;UseFrom&gt;</c>
/// resource is itself a <c>.json</c> document, so mixed-format graphs work without any additional configuration.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Load rules from an embedded XML resource in the entry assembly and construct a service:
/// </para>
/// <code>
///<![CDATA[
/// // The resource is stored as "MyApp/Calendar/Resources/custom-rules.xml" in the assembly manifest:
/// var provider = new XmlResourceNotableDateRuleProvider(
///     "MyApp/Calendar/Resources/custom-rules.xml",
///     new ResourcePathResolver());
///
/// INotableDateService service = new NotableDateService(
///     ruleProviders: new[] { provider },
///     workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);
///
/// // Load from a specific assembly (for example, a companion data assembly):
/// Assembly resourceAssembly = Assembly.Load("MyApp.Resources");
/// var crossAssemblyProvider = new XmlResourceNotableDateRuleProvider(
///     "MyApp/Calendar/Resources/custom-rules.xml",
///     new ResourcePathResolver(),
///     assembly: resourceAssembly);
///
/// // Load from a chain of assemblies (data pack first, main library as fallback for <UseFrom> targets):
/// var packProvider = new XmlResourceNotableDateRuleProvider(
///     "MyApp/Calendar/Resources/region-us.xml",
///     new ResourcePathResolver(),
///     new[] { typeof(MyDataPack).Assembly, typeof(NotableDateService).Assembly });
///]]>
/// </code>
/// </example>
public sealed class XmlResourceNotableDateRuleProvider
    : NotableDateRuleResourceProviderBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="XmlResourceNotableDateRuleProvider" /> class that resolves embedded
    /// resources against a single assembly.
    /// </summary>
    /// <param name="xmlResourceName">
    /// The logical resource path of the root XML payload (e.g.
    /// <c>Bodu/Globalization/Calendar/Resources/global-all.xml</c>). Must not be <see langword="null" />.
    /// </param>
    /// <param name="resourcePathResolver">
    /// The resolver used to translate relative <c>&lt;UseFrom&gt;</c> paths into fully qualified resource names. Must
    /// not be <see langword="null" />.
    /// </param>
    /// <param name="assembly">
    /// The assembly containing the embedded resource(s). Defaults to the currently executing assembly when
    /// <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="xmlResourceName" /> or <paramref name="resourcePathResolver" /> is
    /// <see langword="null" />.
    /// </exception>
    public XmlResourceNotableDateRuleProvider(string xmlResourceName, IResourcePathResolver resourcePathResolver, Assembly? assembly = null)
        : base(xmlResourceName, resourcePathResolver, assembly)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XmlResourceNotableDateRuleProvider" /> class that resolves embedded
    /// resources against an ordered chain of assemblies.
    /// </summary>
    /// <param name="xmlResourceName">
    /// The logical resource path of the root XML payload. Must not be <see langword="null" />.
    /// </param>
    /// <param name="resourcePathResolver">
    /// The resolver used to translate relative <c>&lt;UseFrom&gt;</c> paths into fully qualified resource names. Must
    /// not be <see langword="null" />.
    /// </param>
    /// <param name="assemblies">
    /// The ordered chain of assemblies searched for embedded resources; the first assembly containing a requested
    /// resource wins. Use this overload to layer a companion data pack over the main library, so <c>&lt;UseFrom&gt;</c>
    /// directives can resolve targets that live in a different assembly. Must not be <see langword="null" /> or empty,
    /// and must not contain <see langword="null" /> entries.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="xmlResourceName" />, <paramref name="resourcePathResolver" />, or
    /// <paramref name="assemblies" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="assemblies" /> is empty or contains a <see langword="null" /> entry.
    /// </exception>
    public XmlResourceNotableDateRuleProvider(string xmlResourceName, IResourcePathResolver resourcePathResolver, IEnumerable<Assembly> assemblies)
        : base(xmlResourceName, resourcePathResolver, assemblies)
    {
    }
}
