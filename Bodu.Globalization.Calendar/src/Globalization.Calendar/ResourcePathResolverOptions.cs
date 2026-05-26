// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResourcePathResolverOptions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides options that customize how a <see cref="ResourcePathResolver" /> recognizes and rewrites resource paths.
/// </summary>
/// <remarks>
/// <para>
/// The resolver walks every resource path it is handed and decides whether to leave the path alone or to prepend the
/// canonical resource namespace before consulting an <see cref="System.Resources.ResourceManager" />. A path that
/// already begins with one of the registered <see cref="FullyQualifiedResourcePrefixes" /> is considered authoritative
/// and is returned unchanged; any other path is treated as relative and rewritten to the qualified form.
/// </para>
/// <para>
/// The default prefix set contains only <c>"Bodu.Globalization.Calendar.Resources."</c>. Configure additional prefixes
/// when hosting plugins, satellite assemblies, or shared resource roots whose paths the resolver should pass through
/// untouched. Instances are immutable once constructed; supply a fresh options bag rather than mutating a shared one.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Allow resource paths from a plugin's own namespace through the resolver unchanged.
/// var options = new ResourcePathResolverOptions
/// {
///     FullyQualifiedResourcePrefixes = new HashSet<string>(StringComparer.Ordinal)
///     {
///         "Bodu.Globalization.Calendar.Resources.",
///         "Acme.HolidayPack.Resources.",
///     },
/// };
///]]>
/// </example>
public sealed class ResourcePathResolverOptions
{
    /// <summary>
    /// Gets the set of fully qualified resource prefixes recognized by the resolver.
    /// </summary>
    /// <returns>
    /// An <see cref="IReadOnlySet{T}" /> of case-sensitive, ordinal-compared prefixes. A path that begins with one of
    /// these prefixes is treated as already fully qualified and is returned unchanged.
    /// </returns>
    /// <value>The default set contains <c>"Bodu.Globalization.Calendar.Resources."</c>.</value>
    public IReadOnlySet<string> FullyQualifiedResourcePrefixes { get; init; }
        = new HashSet<string>(StringComparer.Ordinal)
        {
            "Bodu.Globalization.Calendar.Resources.",
        };
}
