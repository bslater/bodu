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
/// effect on output today; <see cref="MaxDepth" /> bounds container nesting.
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
    /// The property is currently inert: the writer emits normalized output that is valid under both TOML v1.0.0 and
    /// v1.1.0 regardless of the value. It is reserved for a future version in which the emitted grammar differs between
    /// specification versions.
    /// </remarks>
    public TomlSpecVersion SpecVersion { get; set; }

    /// <summary>
    /// Gets or sets the maximum container nesting depth the writer will permit.
    /// </summary>
    /// <value>The maximum container nesting depth; <c>0</c> selects the default of 256.</value>
    /// <returns>The maximum container nesting depth, where <c>0</c> selects the default of 256.</returns>
    public int MaxDepth { get; set; }
}
