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
/// <para>
/// Inherits the standard reload-on-change, file-provider, and optional-file behaviours from
/// <see cref="FileConfigurationSource" />, matching the conventions used by the JSON, INI, and XML providers shipped by
/// Microsoft. Three Bodu-specific properties extend the base contract: <see cref="TargetPath" /> drives glob-anchored
/// section matching during resolution, <see cref="ParseOptions" /> controls how the document is read, and
/// <see cref="ResolveOptions" /> controls how the resolved view is layered.
/// </para>
/// <para>
/// Most callers do not construct this type directly; reach for the
/// <see cref="BoduTextConfigurationExtensions.AddBoduConfiguration(IConfigurationBuilder, string, string?, bool, bool)" />
/// extensions instead. Direct construction is appropriate when a host already has the source instance in hand — for
/// example, when wiring a custom <see cref="IConfigurationBuilder" /> programmatically.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Typical lambda registration via the IConfigurationBuilder extension.
/// builder.Configuration.AddBoduConfiguration(source =>
/// {
///     source.Path           = "app.boduconfig";
///     source.TargetPath     = "src/Web/Startup.cs"; // anchors glob sections like [src/**/*.cs]
///     source.Optional       = false;
///     source.ReloadOnChange = true;
///     source.ParseOptions   = BoduConfigurationParseOptions.Strict;
/// });
///
/// // Direct construction — for example, in a custom builder host.
/// var source = new BoduTextConfigurationSource
/// {
///     Path           = "app.boduconfig",
///     ReloadOnChange = true,
///     TargetPath     = "src/Web/Startup.cs",
/// };
/// IConfigurationProvider provider = source.Build(builder);
///]]>
/// </example>
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
