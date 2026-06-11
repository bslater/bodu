// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlWriterOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Writer;

/// <summary>
/// Defines the customizations applied when creating a <see cref="Utf8TomlWriter" />, mirroring the role of
/// <see cref="System.Text.Json.JsonWriterOptions" /> for TOML.
/// </summary>
/// <remarks>
/// <para>
/// TOML output is always canonical — keys are emitted in document order, sub-tables surface as <c>[header]</c> blocks,
/// arrays of tables as <c>[[header]]</c> blocks, and every value uses its shortest round-trippable spelling — so,
/// unlike <see cref="System.Text.Json.JsonWriterOptions" />, there is no indentation or encoder option.
/// </para>
/// <para>
/// The writer always emits text that is valid under both TOML v1.0.0 and v1.1.0; <see cref="SpecVersion" /> selects the
/// few formatting conveniences that differ between the two, and <see cref="MaxDepth" /> bounds container nesting.
/// </para>
/// </remarks>
public struct TomlWriterOptions
{
    /// <summary>
    /// Gets or sets the version of the TOML specification whose formatting conventions the writer applies.
    /// </summary>
    /// <value>The target specification version; the default is <see cref="TomlSpecVersion.V1_0" />.</value>
    /// <returns>The target specification version.</returns>
    public TomlSpecVersion SpecVersion { get; set; }

    /// <summary>
    /// Gets or sets the maximum container nesting depth the writer will permit.
    /// </summary>
    /// <value>The maximum container nesting depth; <c>0</c> selects the default of 256.</value>
    /// <returns>The maximum container nesting depth, where <c>0</c> selects the default of 256.</returns>
    public int MaxDepth { get; set; }
}
