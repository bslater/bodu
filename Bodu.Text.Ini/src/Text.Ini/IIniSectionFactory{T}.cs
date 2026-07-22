// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IIniSectionFactory{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Ini;

/// <summary>
/// Provides reflection-free conversion between a section type and its INI key/value representation, consumed by the
/// <see cref="IniSerializer" /> section overloads.
/// </summary>
/// <typeparam name="TSection">The section type.</typeparam>
/// <remarks>
/// Implementations are typically emitted at compile time by the <c>Bodu.Text.Formats.Generators</c> source generator
/// for types annotated with <see cref="IniSectionAttribute" />, but the interface can equally be implemented by hand
/// when full control over the key mapping is required. Because no member of this contract inspects types at runtime,
/// the factory-based serializer paths are safe for trimming and ahead-of-time compilation.
/// </remarks>
public interface IIniSectionFactory<TSection>
{
    /// <summary>
    /// Gets the key names of the section type, in declaration order.
    /// </summary>
    /// <value>The resolved key names.</value>
    IReadOnlyList<string> Keys { get; }

    /// <summary>
    /// Converts a section instance to its INI entries, in <see cref="Keys" /> order.
    /// </summary>
    /// <param name="section">The section instance.</param>
    /// <returns>The key/value entries.</returns>
    IEnumerable<KeyValuePair<string, string>> GetEntries(TSection section);

    /// <summary>
    /// Creates a section instance from decoded INI entries.
    /// </summary>
    /// <param name="entries">The section's key/value entries, in source order.</param>
    /// <returns>The created section.</returns>
    TSection Create(IEnumerable<KeyValuePair<string, string>> entries);
}
