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
/// enumeration's underlying value, and writing emits the mapped member name or, for the default enum handling, the
/// numeric value per <see cref="YamlSerializerOptions.WriteEnumsAsStrings" />. A null scalar reads as the type default.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
/// <remarks>
/// <para>
/// On write a defined value is emitted as its mapped wire name; a value that does not correspond to a single defined
/// member - an undefined value or a combination of flags - falls back to the runtime's string form of the value, its
/// comma-separated member names for a combination of defined flags and its decimal text otherwise. With
/// <see cref="YamlSerializerOptions.WriteEnumsAsStrings" /> off, the default enum handling writes the underlying value
/// instead, through <see cref="EnumIntegerConversion.WriteInteger{T}(ref Utf8YamlWriter, T)" />, which writes a value
/// of an enumeration based on <see cref="ulong" /> above <see cref="long.MaxValue" /> as its plain invariant text, as a
/// <see cref="ulong" /> member is written. A converter that <see cref="YamlStringEnumConverter" /> or
/// <see cref="YamlStringEnumConverter{TEnum}" /> produces sets <see cref="_alwaysWriteNames" /> and writes names
/// whatever the option says, as System.Text.Json's <c>JsonStringEnumConverter</c> does. On read a scalar is matched
/// case-insensitively against the wire names and then, as a fallback, parsed by the runtime, so combined-flag strings,
/// and numeric strings when integers are allowed, are accepted. An integer scalar maps through the underlying value, by
/// way of <see cref="EnumIntegerConversion.ReadInteger{T}(ref Utf8YamlReader)" />, so one outside the range of the
/// enumeration's underlying type throws <see cref="YamlSerializationException" /> rather than being narrowed into that
/// type.
/// </para>
/// <para>
/// When <see cref="_allowIntegerValues" /> is <see langword="false" /> an integer scalar is rejected rather than mapped
/// through the underlying value, with a message that integer values are not allowed, whether or not the integer is the
/// value of a defined member, and so is any other scalar whose text, as
/// <see cref="ScalarCoercion.ReaderScalarText(ref Utf8YamlReader)" /> gives it, is not a wire name and holds an integer
/// in any of its comma-separated parts, as <see cref="EnumIntegerConversion.ContainsIntegerText(ReadOnlySpan{char})" />
/// determines: a quoted <c>"2"</c>, <c>!!str 2</c>, a plain integer beyond the 64-bit range, which YAML resolves as a
/// string, an integral float such as <c>2.0</c>, or a list such as <c>Read, 2</c>. Each is rejected before the runtime
/// could read the integer, as System.Text.Json's <c>JsonStringEnumConverter</c> rejects a numeric string from .NET 9,
/// and a wire name that is an integer's text, given through <see cref="StringEnumMemberNameAttribute" />, still binds
/// its member. Nor is a value written by name as its decimal text, which the converter would then reject on read: a
/// value that is neither a member nor a combination of defined flags, such as an undefined value or a flags value with
/// an undefined bit, throws <see cref="YamlSerializationException" /> on write, as <c>JsonStringEnumConverter</c>
/// refuses to write it when integers are not allowed.
/// </para>
/// </remarks>
internal sealed class EnumConverter<T>
    : YamlConverter<T>
    where T : struct, Enum
{
    /// <summary>The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.</summary>
    private readonly NamingPolicy? _namingPolicy;

    /// <summary>Whether integer values are allowed: read from an integer scalar or a scalar whose text holds one, and written as the decimal text of a value with no name.</summary>
    private readonly bool _allowIntegerValues;

    /// <summary>Whether a value is written by name whatever <see cref="YamlSerializerOptions.WriteEnumsAsStrings" /> says, as the string enum converters write it.</summary>
    private readonly bool _alwaysWriteNames;

    /// <summary>Maps each wire name to its enumeration value, matched case-insensitively.</summary>
    private readonly Dictionary<string, T> _nameToValue = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Maps each enumeration value to the wire name used when writing it; the first name wins when several members share a value.</summary>
    private readonly Dictionary<T, string> _valueToName = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumConverter{T}" /> class that applies no naming policy, accepts
    /// integer scalars on read, and writes names or integers as
    /// <see cref="YamlSerializerOptions.WriteEnumsAsStrings" /> says, as the default enum handling does.
    /// </summary>
    public EnumConverter()
        : this(namingPolicy: null, allowIntegerValues: true, alwaysWriteNames: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumConverter{T}" /> class.
    /// </summary>
    /// <param name="namingPolicy">
    /// The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.
    /// </param>
    /// <param name="allowIntegerValues">
    /// Whether integer values are allowed: read from an integer scalar or a scalar whose text holds one, and written as
    /// the decimal text of a value that is neither a member nor a combination of flags.
    /// </param>
    /// <param name="alwaysWriteNames">
    /// Whether values are written by name whatever <see cref="YamlSerializerOptions.WriteEnumsAsStrings" /> says, as
    /// the converters the string enum converter factories produce write them; <see langword="false" /> for the default
    /// enum handling, which writes integers when that option is off.
    /// </param>
    /// <remarks>
    /// The name maps are built once at construction by reflecting over the public, static fields of
    /// <typeparamref name="T" />, resolving each member's wire name from its
    /// <see cref="StringEnumMemberNameAttribute" />, then the naming policy, and finally the CLR field name.
    /// </remarks>
    public EnumConverter(NamingPolicy? namingPolicy, bool allowIntegerValues, bool alwaysWriteNames)
    {
        _namingPolicy = namingPolicy;
        _allowIntegerValues = allowIntegerValues;
        _alwaysWriteNames = alwaysWriteNames;

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
                return EnumIntegerConversion.ReadInteger<T>(ref reader);

            throw IntegerNotAllowed(reader.GetInt64().ToString(CultureInfo.InvariantCulture));
        }

        string text = ScalarCoercion.ReaderScalarText(ref reader);
        if (_nameToValue.TryGetValue(text, out T mapped))
            return mapped;

        if (!_allowIntegerValues && EnumIntegerConversion.ContainsIntegerText(text))
            throw IntegerNotAllowed(text);

        return Enum.TryParse(text, ignoreCase: true, out T parsed)
            ? parsed
            : throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_EnumValueNotFound, text, typeof(T)));
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options)
    {
        if (!_alwaysWriteNames && !options.WriteEnumsAsStrings)
        {
            EnumIntegerConversion.WriteInteger(ref writer, value);
            return;
        }

        if (_valueToName.TryGetValue(value, out string? name))
        {
            writer.WriteString(name);
            return;
        }

        // The runtime formats a value that is neither a member nor a combination of defined flags as its decimal text,
        // which this converter would reject on read when it does not allow integer values.
        string text = value.ToString();
        if (!_allowIntegerValues && EnumIntegerConversion.ContainsIntegerText(text))
        {
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_EnumValueNotWritable, text, typeof(T)));
        }

        writer.WriteString(text);
    }

    /// <summary>
    /// Creates the exception reporting that a scalar is, or holds, an integer, which this converter does not allow.
    /// </summary>
    /// <param name="text">The scalar's text: the integer's invariant text, or the text that holds an integer.</param>
    /// <returns>The exception to throw.</returns>
    /// <remarks>
    /// An integer scalar and a scalar whose text holds an integer report the same message, since both are rejected
    /// because integer values are not allowed, whether or not the integer is the value of a defined member.
    /// </remarks>
    private static YamlSerializationException IntegerNotAllowed(string text) =>
        new(string.Format(
            CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_EnumIntegerNotAllowed, text, typeof(T)));
}
