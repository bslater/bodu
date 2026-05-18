// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduTextConfigurationSource.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// A <see cref="FileConfigurationSource" /> that reads a Bodu Text Configuration file and projects its resolved view
/// into the <see cref="IConfiguration" /> hierarchy as colon-delimited keys.
/// </summary>
/// <remarks>
/// Inherits the standard reload-on-change, file-provider, and optional-file behaviours from
/// <see cref="FileConfigurationSource" />, matching the conventions used by the JSON, INI, and XML providers shipped by
/// Microsoft.
/// </remarks>
public sealed class BoduTextConfigurationSource : FileConfigurationSource
{
    /// <summary>
    /// Gets or sets the path used to evaluate glob-anchored sections during resolution. Defaults to
    /// <see langword="null" />, in which case only non-anchored matches and preamble values apply.
    /// </summary>
    /// <returns>The target path supplied to the resolver.</returns>
    public string? TargetPath { get; set; }

    /// <summary>
    /// Gets or sets the parse options applied when the file is loaded.
    /// </summary>
    /// <returns>The parse options, or <see langword="null" /> for the defaults.</returns>
    public BoduConfigurationParseOptions? ParseOptions { get; set; }

    /// <summary>
    /// Gets or sets the resolve options applied when projecting the document into the configuration view.
    /// </summary>
    /// <returns>The resolve options, or <see langword="null" /> for the defaults.</returns>
    public BoduConfigurationResolveOptions? ResolveOptions { get; set; }

    /// <inheritdoc />
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ThrowHelper.ThrowIfNull(builder);
        this.EnsureDefaults(builder);
        return new BoduTextConfigurationProvider(this);
    }
}
