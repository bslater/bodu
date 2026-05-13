// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResourcePathResolverOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides options that customize how a <see cref="ResourcePathResolver" /> recognizes and rewrites resource paths.
/// </summary>
public sealed class ResourcePathResolverOptions
{
    /// <summary>
    /// Gets the set of fully qualified resource prefixes recognized by the resolver.
    /// </summary>
    /// <returns>
    /// An <see cref="IReadOnlySet{T}" /> of case-sensitive, ordinal-compared prefixes. A path that begins with one of these
    /// prefixes is treated as already fully qualified and is returned unchanged.
    /// </returns>
    /// <value>The default set contains <c>"Bodu.Globalization.Calendar.Resources."</c>.</value>
    public IReadOnlySet<string> FullyQualifiedResourcePrefixes { get; init; }
        = new HashSet<string>(StringComparer.Ordinal)
        {
            "Bodu.Globalization.Calendar.Resources."
        };
}