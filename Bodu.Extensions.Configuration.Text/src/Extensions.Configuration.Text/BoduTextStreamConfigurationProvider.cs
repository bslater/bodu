// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduTextStreamConfigurationProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// A <see cref="StreamConfigurationProvider" /> that parses a Bodu Text Configuration stream and populates the
/// configuration data dictionary with the resolved colon-delimited keys.
/// </summary>
/// <remarks>
/// Stream-backed providers are one-shot: the stream is consumed during the initial <see cref="Load(Stream)" />
/// call and no reload-on-change machinery is attached. For file-backed loading with reload support, use
/// <see cref="BoduTextConfigurationProvider" /> instead. The Parse → Resolve → flatten pipeline is shared with
/// <see cref="BoduTextConfigurationProvider" /> via <see cref="BoduTextConfigurationLoader" />.
/// </remarks>
public sealed class BoduTextStreamConfigurationProvider : StreamConfigurationProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoduTextStreamConfigurationProvider" /> class backed by
    /// the supplied source.
    /// </summary>
    /// <param name="source">The source that produced this provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    public BoduTextStreamConfigurationProvider(BoduTextStreamConfigurationSource source)
        : base(source)
    {
        ThrowHelper.ThrowIfNull(source);
        this.BoduSource = source;
    }

    /// <summary>
    /// Gets the typed source that backs this provider.
    /// </summary>
    /// <returns>The originating <see cref="BoduTextStreamConfigurationSource" />.</returns>
    public BoduTextStreamConfigurationSource BoduSource { get; }

    /// <inheritdoc />
    public override void Load(Stream stream) =>
        this.Data = BoduTextConfigurationLoader.LoadData(
            stream,
            this.BoduSource.TargetPath,
            this.BoduSource.ParseOptions,
            this.BoduSource.ResolveOptions);
}
