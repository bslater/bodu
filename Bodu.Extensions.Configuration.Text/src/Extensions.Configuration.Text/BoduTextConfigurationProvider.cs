// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduTextConfigurationProvider.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using Bodu.Text.Configuration;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// A <see cref="FileConfigurationProvider" /> that parses a Bodu Text Configuration file and populates the
/// configuration data dictionary with the resolved colon-delimited keys.
/// </summary>
/// <remarks>
/// The provider inherits change-token, reload-on-change, optional-file, and exception-wrapping behaviour from
/// <see cref="FileConfigurationProvider" />. Override <see cref="Load(Stream)" /> only.
/// </remarks>
public sealed class BoduTextConfigurationProvider : FileConfigurationProvider
{
    /// <summary>
    /// Initializes a new <see cref="BoduTextConfigurationProvider" /> backed by the supplied source.
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
    public override void Load(Stream stream)
    {
        ThrowHelper.ThrowIfNull(stream);

        BoduConfigurationParseOptions parseOptions = this.BoduSource.ParseOptions ?? BoduConfigurationParseOptions.Bodu;
        BoduConfigurationResolveOptions resolveOptions = this.BoduSource.ResolveOptions ?? BoduConfigurationResolveOptions.Bodu;

        BoduConfigurationDocument document = BoduConfigurationDocument.Load(stream, parseOptions, leaveOpen: true);
        BoduConfigurationView view = document.Resolve(this.BoduSource.TargetPath, resolveOptions);

        Dictionary<string, string?> data = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, string?> entry in view)
            data[entry.Key] = entry.Value;

        this.Data = data;
    }
}
