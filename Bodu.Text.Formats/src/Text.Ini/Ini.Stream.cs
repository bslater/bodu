// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Ini.Stream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

public static partial class Ini
{
    /// <summary>
    /// Creates a forward-only <see cref="IniReader" /> over the supplied <see cref="TextReader" /> using default
    /// options.
    /// </summary>
    /// <param name="reader">
    /// The reader to consume INI text from. Owned by the returned <see cref="IniReader" />.
    /// </param>
    /// <returns>A streaming reader positioned before the first entry.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    public static IniReader CreateReader(TextReader reader) =>
        new(reader);

    /// <summary>
    /// Creates a forward-only <see cref="IniReader" /> over the supplied <see cref="TextReader" /> using the specified
    /// options.
    /// </summary>
    /// <param name="reader">
    /// The reader to consume INI text from. Owned by the returned <see cref="IniReader" />.
    /// </param>
    /// <param name="options">Options that control how the source is interpreted.</param>
    /// <returns>A streaming reader positioned before the first entry.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="reader" /> is <see langword="null" />.
    /// </exception>
    public static IniReader CreateReader(TextReader reader, IniParseOptions options) =>
        new(reader, options);

    /// <summary>
    /// Creates a forward-only <see cref="IniWriter" /> over the supplied <see cref="TextWriter" />.
    /// </summary>
    /// <param name="writer">The writer to emit INI text to. Owned by the returned <see cref="IniWriter" />.</param>
    /// <returns>A streaming writer.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="writer" /> is <see langword="null" />.
    /// </exception>
    public static IniWriter CreateWriter(TextWriter writer) =>
        new(writer);
}
