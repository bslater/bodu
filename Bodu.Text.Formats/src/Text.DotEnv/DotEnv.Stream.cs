// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnv.Stream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv;

public static partial class DotEnv
{
    /// <summary>
    /// Creates a forward-only <see cref="DotEnvReader" /> over the supplied <see cref="TextReader" /> using default
    /// options.
    /// </summary>
    /// <param name="reader">
    /// The reader to consume DotEnv text from. Owned by the returned <see cref="DotEnvReader" />.
    /// </param>
    /// <returns>A streaming reader positioned before the first entry.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    public static DotEnvReader CreateReader(TextReader reader) =>
        new(reader);

    /// <summary>
    /// Creates a forward-only <see cref="DotEnvReader" /> over the supplied <see cref="TextReader" /> using the
    /// specified options.
    /// </summary>
    /// <param name="reader">
    /// The reader to consume DotEnv text from. Owned by the returned <see cref="DotEnvReader" />.
    /// </param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <returns>A streaming reader positioned before the first entry.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    public static DotEnvReader CreateReader(TextReader reader, DotEnvParseOptions options) =>
        new(reader, options);

    /// <summary>
    /// Creates a forward-only <see cref="DotEnvWriter" /> over the supplied <see cref="TextWriter" />.
    /// </summary>
    /// <param name="writer">
    /// The writer to emit DotEnv text to. Owned by the returned <see cref="DotEnvWriter" />.
    /// </param>
    /// <returns>A streaming writer.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public static DotEnvWriter CreateWriter(TextWriter writer) =>
        new(writer);
}
