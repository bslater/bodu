// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Document;

/// <summary>
/// Defines the customizations applied when parsing a <see cref="TomlDocument" />.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SpecVersion" /> selects the grammar the parser enforces. The default, <see cref="TomlSpecVersion.V1_0" />
/// , is the most widely deployed released specification; <see cref="TomlSpecVersion.V1_1" /> additionally accepts the
/// TOML v1.1.0 grammar features.
/// </para>
/// <para>
/// <see cref="MaxDepth" /> bounds the nesting of tables and arrays the parser will materialize, guarding against
/// stack-exhausting input. A value of <c>0</c> selects the default of 64, and a larger value is clamped to the hard
/// ceiling <see cref="TomlLimits.AbsoluteMaxDepth" />; a document that nests deeper than the effective limit throws
/// <see cref="TomlFormatException" /> rather than risking a <see cref="StackOverflowException" />.
/// </para>
/// </remarks>
public struct TomlDocumentOptions
{
    /// <summary>
    /// Gets or sets the version of the TOML specification the parser enforces.
    /// </summary>
    /// <value>The specification version applied during parsing.</value>
    public TomlSpecVersion SpecVersion { get; set; }

    /// <summary>
    /// Gets or sets the maximum nesting depth of tables and arrays the parser will accept.
    /// </summary>
    /// <value>
    /// The maximum nesting depth; <c>0</c> selects the default of 64, clamped to
    /// <see cref="TomlLimits.AbsoluteMaxDepth" />.
    /// </value>
    public int MaxDepth { get; set; }
}
