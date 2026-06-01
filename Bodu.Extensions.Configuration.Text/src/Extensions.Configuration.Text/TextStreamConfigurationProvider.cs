// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextStreamConfigurationProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// A <see cref="StreamConfigurationProvider" /> that parses a Bodu Text Configuration stream and populates the
/// configuration data dictionary with the resolved colon-delimited keys.
/// </summary>
/// <remarks>
/// <para>
/// Stream-backed providers are one-shot: the stream is consumed during the initial <see cref="Load(Stream)" /> call and
/// no reload-on-change machinery is attached. For file-backed loading with reload support, use
/// <see cref="TextConfigurationProvider" /> instead. The Parse → Resolve → flatten pipeline is shared with
/// <see cref="TextConfigurationProvider" /> via <see cref="TextConfigurationLoader" />.
/// </para>
/// <para>
/// Consumers do not construct this type directly — it is materialized by
/// <see cref="TextStreamConfigurationSource.Build(IConfigurationBuilder)" /> when an
/// <see cref="IConfigurationBuilder" /> is built. The typed <see cref="TextSource" /> accessor exists for diagnostic
/// scenarios where a host needs to inspect the source that produced a given <see cref="IConfigurationProvider" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Diagnostic introspection: locate the Bodu stream provider after the configuration root has been built.
/// IConfigurationRoot root = builder.Build();
/// TextStreamConfigurationProvider? bodu = root.Providers
///     .OfType<TextStreamConfigurationProvider>()
///     .FirstOrDefault();
///
/// if (bodu is not null)
/// {
///     Console.WriteLine($"Target path: {bodu.TextSource.TargetPath ?? "<none>"}");
/// }
///]]>
/// </example>
public sealed class TextStreamConfigurationProvider : StreamConfigurationProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TextStreamConfigurationProvider" /> class backed by the supplied
    /// source.
    /// </summary>
    /// <param name="source">The source that produced this provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    public TextStreamConfigurationProvider(TextStreamConfigurationSource source)
        : base(source)
    {
        ThrowHelper.ThrowIfNull(source);

        this.TextSource = source;
    }

    /// <summary>
    /// Gets the typed source that backs this provider.
    /// </summary>
    /// <returns>The originating <see cref="TextStreamConfigurationSource" />.</returns>
    public TextStreamConfigurationSource TextSource { get; }

    /// <inheritdoc />
    public override void Load(Stream stream) =>
        this.Data = TextConfigurationLoader.LoadData(
            stream,
            this.TextSource.TargetPath,
            this.TextSource.ParseOptions,
            this.TextSource.ResolveOptions);
}
