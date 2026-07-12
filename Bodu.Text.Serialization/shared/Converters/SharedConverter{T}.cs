// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SharedConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#endif

/// <summary>
/// Bridges the shared converter source to the format's public generic converter base. A <c>using</c> alias cannot name
/// an open generic type, so this is the one place the base class is spelled per format; every shared generic converter
/// derives from this bridge instead.
/// </summary>
/// <typeparam name="T">The converted type.</typeparam>
internal abstract class SharedConverter<T>
#if BENCODE
    : Bodu.Text.Bencode.Serialization.BencodeConverter<T>
#elif TOML
    : Bodu.Text.Toml.Serialization.TomlConverter<T>
#endif
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SharedConverter{T}" /> class.
    /// </summary>
    protected SharedConverter()
    {
    }
}
