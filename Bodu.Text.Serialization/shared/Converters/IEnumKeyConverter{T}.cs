// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IEnumKeyConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#elif YAML
namespace Bodu.Text.Yaml.Serialization.Converters;
#endif

/// <summary>
/// Reads and writes an enumeration used as a dictionary key, as System.Text.Json's enum converter reads and writes it
/// as a property name.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
/// <remarks>
/// A by-name enumeration converter implements this interface so that the keys of a dictionary keyed by its type follow
/// the names it reads and writes values by, its naming policy and its rule for integer values included. A dictionary
/// whose key type has no such converter reads and writes its keys through <see cref="EnumKeyConverter{T}" />.
/// </remarks>
internal interface IEnumKeyConverter<T>
{
    /// <summary>
    /// Attempts to read a dictionary key.
    /// </summary>
    /// <param name="text">The key's text.</param>
    /// <param name="value">
    /// When this method returns <see langword="true" />, the value the key names; otherwise, the default value.
    /// </param>
    /// <returns><see langword="true" /> when the text names a value; otherwise, <see langword="false" />.</returns>
    bool TryReadKey(string text, out T value);

    /// <summary>
    /// Returns the text a dictionary key is written as.
    /// </summary>
    /// <param name="value">The key.</param>
    /// <returns>The key's text.</returns>
    string WriteKey(T value);
}
