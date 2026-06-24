// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationSource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
/// <see cref="TextConfigurationExtensions.AddTextConfigurationFile(IConfigurationBuilder, string, string?, bool, bool)" />
/// extensions instead. Direct construction is appropriate when a host already has the source instance in hand — for
/// example, when wiring a custom <see cref="IConfigurationBuilder" /> programmatically.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Typical lambda registration via the IConfigurationBuilder extension.
/// builder.Configuration.AddTextConfigurationFile(source =>
/// {
///     source.Path           = "app.boduconfig";
///     source.TargetPath     = "src/Web/Startup.cs"; // anchors glob sections like [src/**/*.cs]
///     source.Optional       = false;
///     source.ReloadOnChange = true;
///     source.ParseOptions   = ConfigurationParseOptions.Strict;
/// });
///
/// // Direct construction — for example, in a custom builder host.
/// var source = new TextConfigurationSource
/// {
///     Path           = "app.boduconfig",
///     ReloadOnChange = true,
///     TargetPath     = "src/Web/Startup.cs",
/// };
/// IConfigurationProvider provider = source.Build(builder);
///]]>
/// </example>
public sealed class TextConfigurationSource
    : FileConfigurationSource
{
    /// <summary>
    /// Gets or sets the path used to evaluate glob-anchored sections during resolution. Defaults to
    /// <see langword="null" />, in which case only non-anchored matches and preamble values apply.
    /// </summary>
    /// <value>The target path supplied to the resolver.</value>
    public string? TargetPath { get; set; }

    /// <summary>
    /// Gets or sets the parse options applied when the file is loaded.
    /// </summary>
    /// <value>The parse options, or <see langword="null" /> for the defaults.</value>
    public ConfigurationParseOptions? ParseOptions { get; set; }

    /// <summary>
    /// Gets or sets the resolve options applied when projecting the document into the configuration view.
    /// </summary>
    /// <value>The resolve options, or <see langword="null" /> for the defaults.</value>
    public ConfigurationResolveOptions? ResolveOptions { get; set; }

    /// <inheritdoc />
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ThrowHelper.ThrowIfNull(builder);
        this.EnsureDefaults(builder);
        return new TextConfigurationProvider(this);
    }
}
