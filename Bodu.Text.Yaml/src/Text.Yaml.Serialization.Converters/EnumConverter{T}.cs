// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using Bodu.Text.Serialization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts an enumeration to and from a YAML scalar, honoring a naming policy and any per-member
/// <see cref="StringEnumMemberNameAttribute" />: names are matched case-insensitively, integer scalars map through the
/// enumeration's underlying value, and writing emits the mapped member name or the numeric value per
/// <see cref="YamlSerializerOptions.WriteEnumsAsStrings" />. A null scalar reads as the type default.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
/// <remarks>
/// <para>
/// On write a defined value is emitted as its mapped wire name; a value that does not correspond to a single defined
/// member — an undefined value or a combination of flags — falls back to the value's decimal or comma-separated string
/// form. On read a scalar is matched case-insensitively against the wire names and then, as a fallback, parsed by the
/// runtime so numeric and combined-flag strings are accepted. When <see cref="_allowIntegerValues" /> is
/// <see langword="false" /> an integer scalar is rejected rather than mapped through the underlying value.
/// </para>
/// </remarks>
internal sealed class EnumConverter<T>
    : YamlConverter<T>
    where T : struct, Enum
{
    /// <summary>The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.</summary>
    private readonly NamingPolicy? _namingPolicy;

    /// <summary>Whether an integer scalar is accepted as an enumeration value on read.</summary>
    private readonly bool _allowIntegerValues;

    /// <summary>Maps each wire name to its enumeration value, matched case-insensitively.</summary>
    private readonly Dictionary<string, T> _nameToValue = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps each enumeration value to the wire name used when writing it; the first name wins when several members share a value.</summary>
    private readonly Dictionary<T, string> _valueToName = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumConverter{T}" /> class that applies no naming policy and
    /// accepts integer scalars on read.
    /// </summary>
    public EnumConverter()
        : this(namingPolicy: null, allowIntegerValues: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumConverter{T}" /> class.
    /// </summary>
    /// <param name="namingPolicy">
    /// The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.
    /// </param>
    /// <param name="allowIntegerValues">Whether an integer scalar is accepted as an enumeration value on read.</param>
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
    public override T Read(ref Utf8YamlReader reader, Type typeToConvert, YamlSerializerOptions options)
    {
        if (reader.TokenType == YamlTokenType.Null)
            return default;

        if (reader.TokenType == YamlTokenType.Integer)
        {
            if (_allowIntegerValues)
                return (T)Enum.ToObject(typeof(T), reader.GetInt64());

            string integerText = reader.GetInt64().ToString(CultureInfo.InvariantCulture);
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_EnumValueNotFound, integerText, typeof(T)));
        }

        string text = ScalarCoercion.ReaderScalarText(ref reader);
        if (_nameToValue.TryGetValue(text, out T mapped))
            return mapped;

        return Enum.TryParse(text, ignoreCase: true, out T parsed)
            ? parsed
            : throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_EnumValueNotFound, text, typeof(T)));
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options)
    {
        if (options.WriteEnumsAsStrings)
            writer.WriteString(_valueToName.TryGetValue(value, out string? name) ? name : value.ToString());
        else
            writer.WriteInteger(Convert.ToInt64(value, CultureInfo.InvariantCulture));
    }
}
