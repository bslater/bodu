// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationDocument.Resolve.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

public sealed partial class BoduConfigurationDocument
{
    /// <summary>
    /// Projects the document into a resolved <see cref="BoduConfigurationView" /> for the supplied target
    /// path. Preamble properties and matching section properties are layered in source order.
    /// </summary>
    /// <param name="targetPath">The path the resolved view is evaluated for, or <see langword="null" /> when
    /// no path context is supplied (in which case only the preamble — or non-anchored matches — apply).</param>
    /// <param name="options">The resolve options to apply, or <see langword="null" /> for the defaults.</param>
    /// <returns>A populated <see cref="BoduConfigurationView" />.</returns>
    /// <exception cref="InvalidOperationException">The supplied options require a path root and none was
    /// provided.</exception>
    public BoduConfigurationView Resolve(string? targetPath = null, BoduConfigurationResolveOptions? options = null) =>
        new BoduConfigurationResolver(options ?? BoduConfigurationResolveOptions.Bodu).Resolve(this, targetPath);
}
