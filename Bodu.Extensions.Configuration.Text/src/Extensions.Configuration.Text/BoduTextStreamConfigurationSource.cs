// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduTextStreamConfigurationSource.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration;
using Microsoft.Extensions.Configuration;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// A <see cref="StreamConfigurationSource" /> that reads a Bodu Text Configuration document from an arbitrary
/// <see cref="System.IO.Stream" /> and projects its resolved view into the <see cref="IConfiguration" /> hierarchy as
/// colon-delimited keys.
/// </summary>
/// <remarks>
/// Mirrors the role of <c>JsonStreamConfigurationSource</c> in <c>Microsoft.Extensions.Configuration.Json</c>. Unlike
/// <see cref="BoduTextConfigurationSource" /> (which is file-backed and inherits reload-on-change), this source is
/// one-shot: the stream is parsed once when <see cref="Build(IConfigurationBuilder)" /> is invoked and no file watcher
/// is attached.
/// </remarks>
public sealed class BoduTextStreamConfigurationSource : StreamConfigurationSource
{
    /// <summary>
    /// Gets or sets the path used to evaluate glob-anchored sections during resolution. Defaults to
    /// <see langword="null" />, in which case only non-anchored matches and preamble values apply.
    /// </summary>
    /// <returns>The target path supplied to the resolver.</returns>
    public string? TargetPath { get; set; }

    /// <summary>
    /// Gets or sets the parse options applied when the stream is loaded.
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
        return new BoduTextStreamConfigurationProvider(this);
    }
}
