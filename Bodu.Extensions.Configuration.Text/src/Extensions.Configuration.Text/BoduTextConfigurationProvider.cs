// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduTextConfigurationProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// A <see cref="FileConfigurationProvider" /> that parses a Bodu Text Configuration file and populates the
/// configuration data dictionary with the resolved colon-delimited keys.
/// </summary>
/// <remarks>
/// The provider inherits change-token, reload-on-change, optional-file, and exception-wrapping behaviour from
/// <see cref="FileConfigurationProvider" />. Override <see cref="Load(Stream)" /> only. The Parse → Resolve → flatten
/// pipeline is shared with <see cref="BoduTextStreamConfigurationProvider" /> via
/// <see cref="BoduTextConfigurationLoader" />.
/// </remarks>
public sealed class BoduTextConfigurationProvider : FileConfigurationProvider
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoduTextConfigurationProvider" /> class backed by the supplied
    /// source.
    /// </summary>
    /// <param name="source">The source that produced this provider.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
    public BoduTextConfigurationProvider(BoduTextConfigurationSource source)
        : base(source)
    {
        ThrowHelper.ThrowIfNull(source);
        this.BoduSource = source;
    }

    /// <summary>
    /// Gets the typed source that backs this provider.
    /// </summary>
    /// <returns>The originating <see cref="BoduTextConfigurationSource" />.</returns>
    public BoduTextConfigurationSource BoduSource { get; }

    /// <inheritdoc />
    public override void Load(Stream stream) =>
        this.Data = BoduTextConfigurationLoader.LoadData(
            stream,
            this.BoduSource.TargetPath,
            this.BoduSource.ParseOptions,
            this.BoduSource.ResolveOptions);
}
