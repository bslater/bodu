// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvWriterOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv.Writer;

/// <summary>
/// Provides configuration for a <see cref="Utf8DotEnvWriter" />, controlling how entries are emitted.
/// </summary>
/// <remarks>
/// The options are an immutable value type; assign the <see langword="init" />-only properties through an object
/// initializer and read <see cref="Default" /> for the standard behaviour.
/// </remarks>
public readonly struct DotEnvWriterOptions
{
    /// <summary>
    /// Gets the default writer options.
    /// </summary>
    /// <value>The default options.</value>
    public static DotEnvWriterOptions Default => default;

    /// <summary>
    /// Gets a value indicating whether every written key is prefixed with the <c>export</c> keyword.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to emit an <c>export</c> prefix before each key; otherwise <see langword="false" />.
    /// </value>
    public bool WriteExportPrefix { get; init; }
}
