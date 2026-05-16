// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationSection.Match.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public sealed partial class BoduConfigurationSection
{
    /// <summary>
    /// Determines whether the specified target path matches this section's pattern.
    /// </summary>
    /// <param name="relativePath">The path to test, relative to the document's path root.</param>
    /// <returns><see langword="true" /> when the path matches the pattern. The preamble always returns
    /// <see langword="false" /> here; it is layered separately by the resolver.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="relativePath" /> is <see langword="null" />.</exception>
    public bool IsMatch(string relativePath)
    {
        ThrowHelper.ThrowIfNull(relativePath);

        if (this.IsPreamble || this.Pattern is null)
            return false;

        return BoduConfigurationPattern.Compile(this.Pattern).IsMatch(relativePath);
    }
}
