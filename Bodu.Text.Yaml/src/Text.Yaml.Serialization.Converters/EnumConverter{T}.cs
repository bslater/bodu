// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Serialization;
using Bodu.Text.Yaml.Reader;
using Bodu.Text.Yaml.Writer;

namespace Bodu.Text.Yaml.Serialization.Converters;

/// <summary>
/// Converts an enumeration to and from a YAML scalar, honoring a naming policy and any per-member
/// <see cref="StringEnumMemberNameAttribute" />: names are matched as System.Text.Json's <c>JsonStringEnumConverter</c>
/// matches them, integer scalars map through the enumeration's underlying value, and writing emits the member's name
/// or, for the default enum handling, the numeric value per <see cref="YamlSerializerOptions.WriteEnumsAsStrings" />. A
/// null scalar reads as the type default.
/// </summary>
/// <typeparam name="T">The enumeration type.</typeparam>
/// <remarks>
/// <para>
/// Names are mapped through <see cref="EnumNameTable{T}" />. On write a member is emitted as its wire name, and a
/// combination of defined flags as the comma-separated wire names of its flags, in the order the runtime lists them;
/// any other value, an undefined value or a flags value with an undefined bit, is emitted as its decimal text. With
/// <see cref="YamlSerializerOptions.WriteEnumsAsStrings" /> off, the default enum handling writes the underlying value
/// instead, through <see cref="EnumIntegerConversion.WriteInteger{T}(ref Utf8YamlWriter, T)" />, which writes a value
/// of an enumeration based on <see cref="ulong" /> above <see cref="long.MaxValue" /> as its plain invariant text, as a
/// <see cref="ulong" /> member is written. A converter that <see cref="YamlStringEnumConverter" /> or
/// <see cref="YamlStringEnumConverter{TEnum}" /> produces sets <see cref="_alwaysWriteNames" /> and writes names
/// whatever the option says, as System.Text.Json's <c>JsonStringEnumConverter</c> does.
/// </para>
/// <para>
/// On read a scalar, as <see cref="ScalarCoercion.ReaderScalarText(ref Utf8YamlReader)" /> gives its text, binds when
/// it is a wire name, in that name's own case, or a comma-separated list of names, in any order, each of which is a
/// wire name or the CLR name, in any case, of a member that <see cref="StringEnumMemberNameAttribute" /> does not
/// rename. An integer scalar maps through the underlying value, by way of
/// <see cref="EnumIntegerConversion.ReadInteger{T}(ref Utf8YamlReader)" />, so one outside the range of the
/// enumeration's underlying type throws <see cref="YamlSerializationException" /> rather than being narrowed into that
/// type, and a scalar whose whole text is an integer, such as a quoted <c>"2"</c>, binds as the value it holds. An
/// integer inside a list never binds, and a float scalar, even an integral one such as <c>2.0</c>, never binds, as
/// <c>JsonStringEnumConverter</c> never binds a number that is not an integer.
/// </para>
/// <para>
/// When <see cref="_allowIntegerValues" /> is <see langword="false" /> an integer scalar is rejected rather than mapped
/// through the underlying value, with a message that integer values are not allowed, whether or not the integer is the
/// value of a defined member, and so is any other scalar that is not a name and holds an integer in any of its
/// comma-separated parts, as <see cref="EnumIntegerConversion.ContainsIntegerText(ReadOnlySpan{char})" /> determines: a
/// quoted <c>"2"</c>, <c>!!str 2</c>, a plain integer beyond the 64-bit range, which YAML resolves as a string, or a
/// list such as <c>Read, 2</c>, as System.Text.Json's <c>JsonStringEnumConverter</c> rejects a numeric string from .NET
/// 9. A wire name that is an integer's text, given through <see cref="StringEnumMemberNameAttribute" />, still binds
/// its member. Nor is a value written by name as its decimal text, which the converter would then reject on read: a
/// value that is neither a member nor a combination of defined flags throws <see cref="YamlSerializationException" />
/// on write, as <c>JsonStringEnumConverter</c> refuses to write it when integers are not allowed.
/// </para>
/// <para>
/// The keys of a dictionary keyed by <typeparamref name="T" /> are read and written by the same names and the same rule
/// for integer values, through <see cref="IEnumKeyConverter{T}" />, as System.Text.Json's enum converter reads and
/// writes property names: a key is always written by name, whatever
/// <see cref="YamlSerializerOptions.WriteEnumsAsStrings" /> says, and a key whose value has no name is written as its
/// decimal text whatever the rule for integer values says.
/// </para>
/// </remarks>
internal sealed class EnumConverter<T>
    : YamlConverter<T>, IEnumKeyConverter<T>
    where T : struct, Enum
{
    /// <summary>The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.</summary>
    private readonly NamingPolicy? _namingPolicy;

    /// <summary>Whether integer values are allowed: read from an integer scalar or a scalar whose text is one, and written as the decimal text of a value with no name.</summary>
    private readonly bool _allowIntegerValues;

    /// <summary>Whether a value is written by name whatever <see cref="YamlSerializerOptions.WriteEnumsAsStrings" /> says, as the string enum converters write it.</summary>
    private readonly bool _alwaysWriteNames;

    /// <summary>The members' wire names, and the CLR names that also bind, in both directions.</summary>
    private readonly EnumNameTable<T> _names;

    /// <summary>Reads and writes the keys of a dictionary keyed by <typeparamref name="T" />.</summary>
    private readonly EnumKeyConverter<T> _keys;

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
    /// Whether integer values are allowed: read from an integer scalar or a scalar whose text is one, and written as
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
        _names = new EnumNameTable<T>(namingPolicy);
        _keys = new EnumKeyConverter<T>(_names, allowIntegerValues);
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

        if (reader.TokenType == YamlTokenType.Float)
        {
            throw new YamlSerializationException(string.Format(
                CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_EnumFloatNotAllowed, typeof(T)));
        }

        string text = ScalarCoercion.ReaderScalarText(ref reader);
        if (_names.TryParse(text, out T named))
            return named;

        if (EnumIntegerConversion.IsIntegerText(text) && _allowIntegerValues)
        {
            return Enum.TryParse(text, out T parsed)
                ? parsed
                : throw ValueNotFound(text);
        }

        if (!_allowIntegerValues && EnumIntegerConversion.ContainsIntegerText(text))
            throw IntegerNotAllowed(text);

        throw ValueNotFound(text);
    }

    /// <inheritdoc />
    public override void Write(Utf8YamlWriter writer, T value, YamlSerializerOptions options)
    {
        if (!_alwaysWriteNames && !options.WriteEnumsAsStrings)
        {
            EnumIntegerConversion.WriteInteger(ref writer, value);
            return;
        }

        // A value that is neither a member nor a combination of defined flags has only its decimal text, which this
        // converter would reject on read when it does not allow integer values.
        if (!_names.TryFormat(value, out string text) && !_allowIntegerValues)
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

    /// <summary>
    /// Creates the exception reporting that a scalar's text is neither a name, a list of names, nor an integer this
    /// converter reads.
    /// </summary>
    /// <param name="text">The scalar's text.</param>
    /// <returns>The exception to throw.</returns>
    private static YamlSerializationException ValueNotFound(string text) =>
        new(string.Format(
            CultureInfo.CurrentCulture, YamlResourceStrings.Op_Invalid_EnumValueNotFound, text, typeof(T)));

    /// <inheritdoc />
    bool IEnumKeyConverter<T>.TryReadKey(string text, out T value) =>
        _keys.TryReadKey(text, out value);

    /// <inheritdoc />
    string IEnumKeyConverter<T>.WriteKey(T value) =>
        _keys.WriteKey(value);
}
