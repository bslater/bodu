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
/// extensions instead. Direct construction is appropriate when a host already has the source instance in hand - for
/// example, when wiring a custom <see cref="IConfigurationBuilder" /> programmatically.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
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
/// // Direct construction - for example, in a custom builder host.
/// var source = new TextConfigurationSource
/// {
///     Path           = "app.boduconfig",
///     ReloadOnChange = true,
///     TargetPath     = "src/Web/Startup.cs",
/// };
/// IConfigurationProvider provider = source.Build(builder);
///]]>
/// </code>
/// </example>
public sealed class TextConfigurationSource
    : FileConfigurationSource
{
    /// <summary>
    /// Gets or sets the path that each section's glob is matched against during resolution. Defaults to
    /// <see langword="null" />, in which case no section applies and only the preamble contributes (when
    /// <see cref="ConfigurationResolveOptions.ApplyPreambleProperties" /> is set).
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
    /// <remarks>
    /// The source locates <see cref="FileConfigurationSource.Path" /> as <c>AddJsonFile</c> does: when no
    /// <see cref="FileConfigurationSource.FileProvider" /> is set, a rooted path is served by a
    /// <see cref="Microsoft.Extensions.FileProviders.PhysicalFileProvider" /> at the deepest directory on it that
    /// exists (see <see cref="FileConfigurationSource.ResolveFileProvider" />), and any other path by the builder's
    /// file provider, which is the provider <c>SetFileProvider</c> sets, the base path <c>SetBasePath</c> sets, or by
    /// default the application's base directory.
    /// </remarks>
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ThrowHelper.ThrowIfNull(builder);

        // A no-op unless the path is rooted and no file provider was supplied; without it the builder's provider
        // would look a rooted path up under its own root.
        ResolveFileProvider();
        this.EnsureDefaults(builder);
        return new TextConfigurationProvider(this);
    }
}
