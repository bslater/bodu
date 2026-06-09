// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextConfigurationProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// A <see cref="FileConfigurationProvider" /> that parses a Bodu Text Configuration file and populates the
/// configuration data dictionary with the resolved colon-delimited keys.
/// </summary>
/// <remarks>
/// <para>
/// The provider inherits change-token, reload-on-change, optional-file, and exception-wrapping behaviour from
/// <see cref="FileConfigurationProvider" />. Override <see cref="Load(Stream)" /> only. The Parse → Resolve → flatten
/// pipeline is shared with <see cref="TextStreamConfigurationProvider" /> via <see cref="TextConfigurationLoader" />.
/// </para>
/// <para>
/// Consumers do not construct this type directly — it is materialized by
/// <see cref="TextConfigurationSource.Build(IConfigurationBuilder)" /> when an <see cref="IConfigurationBuilder" /> is
/// built. The typed <see cref="TextSource" /> accessor exists for diagnostic scenarios where a host needs to inspect
/// the source that produced a given <see cref="IConfigurationProvider" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Diagnostic introspection: locate the Bodu provider after the configuration root has been built.
/// IConfigurationRoot root = builder.Build();
/// TextConfigurationProvider? bodu = root.Providers
///     .OfType<TextConfigurationProvider>()
///     .FirstOrDefault();
///
/// if (bodu is not null)
/// {
///     Console.WriteLine($"Loaded from: {bodu.TextSource.Path}");
///     Console.WriteLine($"Reload on change: {bodu.TextSource.ReloadOnChange}");
/// }
///]]>
/// </example>
public sealed class TextConfigurationProvider
    : FileConfigurationProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextConfigurationProvider" /> class backed by the supplied source.
    /// </summary>
    /// <param name="source">The source that produced this provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    public TextConfigurationProvider(TextConfigurationSource source)
        : base(source)
    {
        ThrowHelper.ThrowIfNull(source);
        this.TextSource = source;
    }

    /// <summary>
    /// Gets the typed source that backs this provider.
    /// </summary>
    /// <returns>The originating <see cref="TextConfigurationSource" />.</returns>
    public TextConfigurationSource TextSource { get; }

    /// <inheritdoc />
    public override void Load(Stream stream) =>
        this.Data = TextConfigurationLoader.LoadData(
            stream,
            this.TextSource.TargetPath,
            this.TextSource.ParseOptions,
            this.TextSource.ResolveOptions);
}
