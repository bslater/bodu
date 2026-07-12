// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using System.Reflection;

#if BENCODE
namespace Bodu.Text.Bencode.Serialization.Converters;
#elif TOML
namespace Bodu.Text.Toml.Serialization.Converters;
#endif

/// <summary>
/// Converts an enumeration value to and from the format's string token holding its member name, honoring a naming
/// policy and any per-member <see cref="StringEnumMemberNameAttribute" />, and optionally accepting an integer on read.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
/// <remarks>
/// <para>
/// On write a defined value is emitted as its mapped member name; a value that does not correspond to a single defined
/// member — an undefined value or a combination of flags — falls back to the value's decimal or comma-separated string
/// form. On read a string is matched case-insensitively against the member names and then, as a fallback, parsed by the
/// runtime so numeric and combined-flag strings are accepted. When <see cref="_allowIntegerValues" /> is
/// <see langword="true" /> an integer token is also accepted and converted to the underlying enumeration value.
/// </para>
/// </remarks>
internal sealed class EnumConverter<T>
    : SharedConverter<T>
    where T : struct, Enum
{
    /// <summary>The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.</summary>
    private readonly NamingPolicy? _namingPolicy;

    /// <summary>Whether an integer token is accepted as an enumeration value on read.</summary>
    private readonly bool _allowIntegerValues;

    /// <summary>Maps each wire name to its enumeration value, matched case-insensitively.</summary>
    private readonly Dictionary<string, T> _nameToValue = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps each enumeration value to the wire name used when writing it; the first name wins when several members share a value.</summary>
    private readonly Dictionary<T, string> _valueToName = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumConverter{T}" /> class.
    /// </summary>
    /// <param name="namingPolicy">
    /// The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.
    /// </param>
    /// <param name="allowIntegerValues">Whether an integer token is accepted as an enumeration value on read.</param>
    /// <remarks>
    /// The name maps are built once at construction by reflecting over the public, static fields of
    /// <typeparamref name="T" />, resolving each member's wire name from its
    /// <see cref="StringEnumMemberNameAttribute" />, then the naming policy, and finally the CLR field name.
    /// </remarks>
    public EnumConverter(NamingPolicy? namingPolicy, bool allowIntegerValues)
    {
        _namingPolicy = namingPolicy;
        _allowIntegerValues = allowIntegerValues;

        foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            string name = field.GetCustomAttribute<StringEnumMemberNameAttribute>()?.Name
                ?? namingPolicy?.ConvertName(field.Name)
                ?? field.Name;
            var value = (T)field.GetValue(null)!;

            _nameToValue[name] = value;
            if (!_valueToName.ContainsKey(value))
                _valueToName[value] = name;
        }
    }

    /// <inheritdoc />
    public override T Read(ref FormatReader reader, Type typeToConvert, FormatOptions options)
    {
        if (reader.TokenType == FormatToken.String)
        {
            string text = reader.GetString();
            if (_nameToValue.TryGetValue(text, out T mapped))
                return mapped;

            return Enum.TryParse(text, ignoreCase: true, out T parsed)
                ? parsed
                : throw SerializationThrowHelper.EnumValueNotFound(ref reader, text, typeof(T));
        }

        if (reader.TokenType == FormatToken.Integer && _allowIntegerValues)
            return (T)Enum.ToObject(typeof(T), reader.GetInt64());

        throw SerializationThrowHelper.ExpectedString(ref reader);
    }

    /// <inheritdoc />
    public override void Write(FormatWriter writer, T value, FormatOptions options) =>
        writer.WriteString(_valueToName.TryGetValue(value, out string? name) ? name : value.ToString());
}
