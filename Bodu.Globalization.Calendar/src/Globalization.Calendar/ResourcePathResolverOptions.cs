namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides options used by <see cref="ResourcePathResolver" />.
/// </summary>
public sealed class ResourcePathResolverOptions
{
    /// <summary>
    /// Gets or initializes the fully qualified resource prefixes recognised by the resolver.
    /// </summary>
    public IReadOnlySet<string> FullyQualifiedResourcePrefixes { get; init; }
        = new HashSet<string>(StringComparer.Ordinal)
        {
            "Bodu.Globalization.Calendar.Resources."
        };
}