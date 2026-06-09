// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlReaderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Configures how a <see cref="TomlReader" /> parses TOML text. The options are immutable once constructed and may be
/// shared across reads.
/// </summary>
/// <remarks>
/// The default-constructed instance parses strict TOML v1.0.0. Set <see cref="SpecVersion" /> to
/// <see cref="TomlSpecVersion.V1_1" /> to opt in to the additional TOML v1.1.0 grammar features.
/// </remarks>
/// <example>
///<![CDATA[
/// var options = new TomlReaderOptions { SpecVersion = TomlSpecVersion.V1_1 };
/// TomlTable document = new TomlReader(options).Read("lt = 07:32");
///]]>
/// </example>
public sealed class TomlReaderOptions
{
    /// <summary>
    /// Gets the TOML specification version the reader enforces.
    /// </summary>
    /// <value>The specification version applied during parsing.</value>
    /// <returns>
    /// The <see cref="TomlSpecVersion" /> the reader enforces; <see cref="TomlSpecVersion.V1_0" /> by default.
    /// </returns>
    public TomlSpecVersion SpecVersion { get; init; } = TomlSpecVersion.V1_0;
}
