// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonResourceNotableDateRuleProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Loads a graph of <see cref="NotableDateRule" /> instances from an embedded JSON resource, recursively resolving
/// every cherry-pick directive declared via <c>useFrom</c> / <c>uses</c> / <c>useAll</c>.
/// </summary>
/// <remarks>
/// <para>
/// Companion to <see cref="XmlResourceNotableDateRuleProvider" /> for authors who prefer JSON. Both providers share the
/// flatten pipeline implemented in <see cref="NotableDateRuleResourceProviderBase" />, so the same cherry-pick
/// semantics apply: explicit opt-in (rules never cascade unless listed), per-directive overrides via the optional
/// nested <c>rule</c> body, and locally declared rules winning over inherited ones with the same composite key.
/// </para>
/// <para>
/// The base provider dispatches per-resource by file extension, so a JSON document may <c>useFrom</c> an XML resource
/// (and vice versa) without any additional configuration. Pick this class when the root resource is JSON; pick
/// <see cref="XmlResourceNotableDateRuleProvider" /> when the root is XML. The two are otherwise interchangeable.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Load rules from an embedded JSON resource:
/// </para>
/// <code>
///<![CDATA[
/// var provider = new JsonResourceNotableDateRuleProvider(
///     "MyApp/Calendar/Resources/custom-rules.json",
///     new ResourcePathResolver());
///
/// INotableDateService service = new NotableDateService(
///     ruleProviders: new[] { provider },
///     workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);
///]]>
/// </code>
/// </example>
public sealed class JsonResourceNotableDateRuleProvider
    : NotableDateRuleResourceProviderBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonResourceNotableDateRuleProvider" /> class that resolves
    /// embedded resources against a single assembly.
    /// </summary>
    /// <param name="jsonResourceName">
    /// The logical resource path of the root JSON payload (for example
    /// <c>MyApp/Calendar/Resources/custom-rules.json</c>). Must not be <see langword="null" />.
    /// </param>
    /// <param name="resourcePathResolver">
    /// The resolver used to translate relative <c>useFrom</c> paths into fully qualified resource names. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="assembly">
    /// The assembly containing the embedded resource(s). Defaults to the currently executing assembly when
    /// <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="jsonResourceName" /> or <paramref name="resourcePathResolver" /> is
    /// <see langword="null" />.
    /// </exception>
    public JsonResourceNotableDateRuleProvider(string jsonResourceName, IResourcePathResolver resourcePathResolver, Assembly? assembly = null)
        : base(jsonResourceName, resourcePathResolver, assembly)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonResourceNotableDateRuleProvider" /> class that resolves
    /// embedded resources against an ordered chain of assemblies.
    /// </summary>
    /// <param name="jsonResourceName">
    /// The logical resource path of the root JSON payload. Must not be <see langword="null" />.
    /// </param>
    /// <param name="resourcePathResolver">
    /// The resolver used to translate relative <c>useFrom</c> paths into fully qualified resource names. Must not be
    /// <see langword="null" />.
    /// </param>
    /// <param name="assemblies">
    /// The ordered chain of assemblies searched for embedded resources; the first assembly containing a requested
    /// resource wins. Must not be <see langword="null" /> or empty, and must not contain <see langword="null" />
    /// entries.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="jsonResourceName" />, <paramref name="resourcePathResolver" />, or
    /// <paramref name="assemblies" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="assemblies" /> is empty or contains a <see langword="null" /> entry.
    /// </exception>
    public JsonResourceNotableDateRuleProvider(string jsonResourceName, IResourcePathResolver resourcePathResolver, IEnumerable<Assembly> assemblies)
        : base(jsonResourceName, resourcePathResolver, assemblies)
    {
    }
}
