// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumKeyConverter{T}.cs" company="Bodu Pty. Ltd.">
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
/// Reads and writes an enumeration used as a dictionary key by its members' wire names, as System.Text.Json's enum
/// converter reads and writes a property name.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
/// <remarks>
/// <para>
/// A key is written by its wire name, a combination of flags by its flags' wire names, and any other value by its
/// decimal text, as <see cref="EnumNameTable{T}.TryFormat(T, out string)" /> formats them. A key is read as
/// <see cref="EnumNameTable{T}.TryParse(string, out T)" /> reads a name or a list of names; failing that, integer text
/// binds as a whole where integer values are allowed, as long as it fits the underlying type.
/// </para>
/// <para>
/// The parameterless constructor gives the keys of an enumeration that no by-name converter handles: wire names with no
/// naming policy, and integer values allowed, as System.Text.Json reads and writes such keys.
/// </para>
/// </remarks>
internal sealed class EnumKeyConverter<T>
    : IEnumKeyConverter<T>
    where T : struct, Enum
{
    /// <summary>The names the keys are read and written by.</summary>
    private readonly EnumNameTable<T> _names;

    /// <summary>Whether integer text binds as a key.</summary>
    private readonly bool _allowIntegerValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumKeyConverter{T}" /> class that reads and writes keys by the
    /// members' wire names with no naming policy, and allows integer values.
    /// </summary>
    public EnumKeyConverter()
        : this(new EnumNameTable<T>(namingPolicy: null), allowIntegerValues: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumKeyConverter{T}" /> class over an enumeration converter's
    /// names.
    /// </summary>
    /// <param name="names">The names the keys are read and written by.</param>
    /// <param name="allowIntegerValues">Whether integer text binds as a key.</param>
    public EnumKeyConverter(EnumNameTable<T> names, bool allowIntegerValues)
    {
        _names = names;
        _allowIntegerValues = allowIntegerValues;
    }

    /// <inheritdoc />
    public bool TryReadKey(string text, out T value)
    {
        if (_names.TryParse(text, out value))
            return true;

        // Enum.TryParse binds the integer text as a whole and fails for a value outside the underlying type's range.
        if (_allowIntegerValues && EnumIntegerConversion.IsIntegerText(text) && Enum.TryParse(text, out value))
            return true;

        value = default;
        return false;
    }

    /// <inheritdoc />
    public string WriteKey(T value)
    {
        // A value that is neither a member nor a combination of defined flags is written as its decimal text, whatever
        // the converter's rule for integer values, as System.Text.Json writes it.
        _ = _names.TryFormat(value, out string text);
        return text;
    }
}
