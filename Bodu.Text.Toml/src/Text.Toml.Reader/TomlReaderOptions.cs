// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml.Reader;

/// <summary>
/// Defines the customizations applied when creating a <see cref="TomlDocumentReader" />, mirroring the role of
/// <see cref="System.Text.Json.JsonReaderOptions" /> for TOML.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SpecVersion" /> selects the grammar the reader enforces. The default, <see cref="TomlSpecVersion.V1_0" />
/// , is the most widely deployed released specification; <see cref="TomlSpecVersion.V1_1" /> additionally accepts the
/// TOML v1.1.0 grammar features.
/// </para>
/// <para>
/// <see cref="MaxDepth" /> bounds the nesting of tables and arrays the reader will materialize, guarding against
/// stack-exhausting input. A value of <c>0</c> selects the default of 256.
/// </para>
/// </remarks>
public struct TomlReaderOptions
{
    /// <summary>
    /// Gets or sets the version of the TOML specification the reader enforces.
    /// </summary>
    /// <value>The specification version applied during parsing.</value>
    /// <returns>
    /// The <see cref="TomlSpecVersion" /> the reader enforces; <see cref="TomlSpecVersion.V1_0" /> by default.
    /// </returns>
    public TomlSpecVersion SpecVersion { get; set; }

    /// <summary>
    /// Gets or sets the maximum nesting depth of tables and arrays the reader will accept.
    /// </summary>
    /// <value>The maximum nesting depth; <c>0</c> selects the default of 256.</value>
    /// <returns>The maximum nesting depth, where <c>0</c> selects the default of 256.</returns>
    public int MaxDepth { get; set; }
}
