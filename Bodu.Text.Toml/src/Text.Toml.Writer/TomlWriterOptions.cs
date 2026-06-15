// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlWriterOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Writer;

/// <summary>
/// Defines the customizations applied when creating a <see cref="Utf8TomlWriter" />.
/// </summary>
/// <remarks>
/// <para>
/// TOML output always uses a normalized layout — keys are emitted in insertion order, sub-tables surface as
/// <c>[header]</c> blocks, arrays of tables as <c>[[header]]</c> blocks, and every value uses its shortest
/// round-trippable spelling — so there is no indentation or encoder option. The layout is deterministic for a given
/// insertion order, but two semantically equal documents built in different orders serialize to different bytes; it is
/// not a canonical form suitable for hashing or signing.
/// </para>
/// <para>
/// The writer always emits text that is valid under both TOML v1.0.0 and v1.1.0, so <see cref="SpecVersion" /> has no
/// effect on output today; <see cref="MaxDepth" /> bounds container nesting, guarding against stack-exhausting graphs.
/// A value of <c>0</c> selects the default of 64, and a larger value is clamped to the hard ceiling
/// <see cref="TomlLimits.AbsoluteMaxDepth" />; opening a table or array deeper than the effective limit throws
/// <see cref="TomlSerializationException" /> rather than risking a <see cref="StackOverflowException" />.
/// </para>
/// </remarks>
public struct TomlWriterOptions
{
    /// <summary>
    /// Gets or sets the version of the TOML specification associated with the writer.
    /// </summary>
    /// <value>The target specification version; the default is <see cref="TomlSpecVersion.V1_0" />.</value>
    /// <returns>The target specification version.</returns>
    /// <remarks>
    /// The property has no effect: the writer emits normalized output that is valid under both TOML v1.0.0 and v1.1.0
    /// regardless of the value. It is obsolete and will be removed in a future release.
    /// </remarks>
    [Obsolete("The writer emits output valid under both TOML v1.0.0 and v1.1.0, so SpecVersion has no effect; it will be removed in a future release.")]
    public TomlSpecVersion SpecVersion { get; set; }

    /// <summary>
    /// Gets or sets the maximum container nesting depth the writer will permit.
    /// </summary>
    /// <value>
    /// The maximum container nesting depth; <c>0</c> selects the default of 64, clamped to
    /// <see cref="TomlLimits.AbsoluteMaxDepth" />.
    /// </value>
    /// <returns>The maximum container nesting depth, where <c>0</c> selects the default of 64.</returns>
    public int MaxDepth { get; set; }
}
