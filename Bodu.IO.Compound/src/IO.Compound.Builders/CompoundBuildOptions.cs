// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundBuildOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.Builders;

/// <summary>
/// Specifies options that control how a <see cref="CompoundStorageBuilder" /> tree serializes a compound file.
/// </summary>
/// <remarks>
/// The default value (<see langword="default" />) selects version 3 (512-byte sectors) and the default maximum nesting
/// depth, producing the most widely compatible output.
/// </remarks>
public struct CompoundBuildOptions
{
    /// <summary>The built-in maximum storage nesting depth applied when <see cref="MaxDepth" /> is zero.</summary>
    internal const int DefaultMaxDepth = 64;

    /// <summary>
    /// Gets or sets the compound-file format version to write.
    /// </summary>
    /// <value>
    /// The target <see cref="CompoundFileVersion" />; <see cref="CompoundFileVersion.V3" /> when left at its default.
    /// </value>
    public CompoundFileVersion Version { get; set; }

    /// <summary>
    /// Gets or sets the maximum storage nesting depth permitted while writing.
    /// </summary>
    /// <value>The configured maximum depth, or <c>0</c> to use the built-in default.</value>
    public int MaxDepth { get; set; }

    /// <summary>
    /// Gets the regular sector size, in bytes, implied by <see cref="Version" />.
    /// </summary>
    /// <value><c>4096</c> for version 4; otherwise <c>512</c>.</value>
    internal readonly int SectorSize =>
        Version == CompoundFileVersion.V4 ? 4096 : 512;

    /// <summary>
    /// Gets the effective maximum nesting depth, resolving the default when unset.
    /// </summary>
    /// <value>
    /// The configured <see cref="MaxDepth" /> when positive; otherwise <see cref="DefaultMaxDepth" />.
    /// </value>
    internal readonly int EffectiveMaxDepth =>
        MaxDepth > 0 ? MaxDepth : DefaultMaxDepth;
}
