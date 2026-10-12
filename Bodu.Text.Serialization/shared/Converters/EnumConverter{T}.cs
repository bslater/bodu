// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

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
/// Names are mapped as System.Text.Json's <c>JsonStringEnumConverter</c> maps them, through
/// <see cref="EnumNameTable{T}" />. On write a member is emitted as its wire name, and a combination of defined flags
/// as the comma-separated wire names of its flags, in the order the runtime lists them; any other value, an undefined
/// value or a flags value with an undefined bit, is emitted as its decimal text. On read a string binds when it is a
/// wire name, in that name's own case, or a comma-separated list of names, in any order, each of which is a wire name
/// or the CLR name, in any case, of a member that <see cref="StringEnumMemberNameAttribute" /> does not rename. When
/// <see cref="_allowIntegerValues" /> is <see langword="true" /> a string whose whole text is an integer also binds, as
/// does an integer token, through <see cref="EnumIntegerConversion.ReadInteger{T}(ref FormatReader)" />: an integer
/// outside the range of the enumeration's underlying type throws the format's serialization exception rather than being
/// narrowed into that type. An integer inside a list never binds.
/// </para>
/// <para>
/// The keys of a dictionary keyed by <typeparamref name="T" /> are read and written by the same names and the same rule
/// for integer values, through <see cref="IEnumKeyConverter{T}" />, as System.Text.Json's enum converter reads and
/// writes property names: a key whose value has no name is written as its decimal text whatever that rule says.
/// </para>
/// <para>
/// When <see cref="_allowIntegerValues" /> is <see langword="false" /> an integer token throws, and so does a string
/// that is not a name and holds an integer in any of its comma-separated parts, such as <c>2</c>, <c> +2 </c> or
/// <c>Read, 2</c>, as <see cref="EnumIntegerConversion.ContainsIntegerText(ReadOnlySpan{char})" /> determines: the
/// format's serialization exception reports that integer values are not allowed, as System.Text.Json's
/// <c>JsonStringEnumConverter</c> rejects such a string from .NET 9. A wire name that is an integer's text, given
/// through <see cref="StringEnumMemberNameAttribute" />, still binds its member, because the names are matched first.
/// Nor is a value written as its decimal text, which the converter would then reject on read: a value that is neither a
/// member nor a combination of defined flags throws the format's serialization exception on write, as
/// <c>JsonStringEnumConverter</c> refuses to write it when integers are not allowed.
/// </para>
/// </remarks>
internal sealed class EnumConverter<T>
    : SharedConverter<T>, IEnumKeyConverter<T>
    where T : struct, Enum
{
    /// <summary>The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.</summary>
    private readonly NamingPolicy? _namingPolicy;

    /// <summary>Whether integer values are allowed: read from an integer token or a string that is one, and written as the decimal text of a value with no name.</summary>
    private readonly bool _allowIntegerValues;

    /// <summary>The members' wire names, and the CLR names that also bind, in both directions.</summary>
    private readonly EnumNameTable<T> _names;

    /// <summary>Reads and writes the keys of a dictionary keyed by <typeparamref name="T" />.</summary>
    private readonly EnumKeyConverter<T> _keys;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnumConverter{T}" /> class.
    /// </summary>
    /// <param name="namingPolicy">
    /// The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.
    /// </param>
    /// <param name="allowIntegerValues">
    /// Whether integer values are allowed: read from an integer token or a string that is one, and written as the
    /// decimal text of a value that is neither a member nor a combination of flags.
    /// </param>
    /// <remarks>
    /// The name maps are built once at construction by reflecting over the public, static fields of
    /// <typeparamref name="T" />, resolving each member's wire name from its
    /// <see cref="StringEnumMemberNameAttribute" />, then the naming policy, and finally the CLR field name.
    /// </remarks>
    public EnumConverter(NamingPolicy? namingPolicy, bool allowIntegerValues)
    {
        _namingPolicy = namingPolicy;
        _allowIntegerValues = allowIntegerValues;
        _names = new EnumNameTable<T>(namingPolicy);
        _keys = new EnumKeyConverter<T>(_names, allowIntegerValues);
    }

    /// <inheritdoc />
    public override T Read(ref FormatReader reader, Type typeToConvert, FormatOptions options)
    {
        if (reader.TokenType == FormatToken.String)
        {
            string text = reader.GetString();
            if (_names.TryParse(text, out T named))
                return named;

            if (EnumIntegerConversion.IsIntegerText(text) && _allowIntegerValues)
            {
                return Enum.TryParse(text, out T parsed)
                    ? parsed
                    : throw SerializationThrowHelper.EnumValueNotFound(ref reader, text, typeof(T));
            }

            if (!_allowIntegerValues && EnumIntegerConversion.ContainsIntegerText(text))
                throw SerializationThrowHelper.EnumIntegerNotAllowed(ref reader, text, typeof(T));

            throw SerializationThrowHelper.EnumValueNotFound(ref reader, text, typeof(T));
        }

        if (reader.TokenType == FormatToken.Integer && _allowIntegerValues)
            return EnumIntegerConversion.ReadInteger<T>(ref reader);

        throw SerializationThrowHelper.ExpectedString(ref reader);
    }

    /// <inheritdoc />
    public override void Write(FormatWriter writer, T value, FormatOptions options)
    {
        // A value that is neither a member nor a combination of defined flags has only its decimal text, which this
        // converter would reject on read when it does not allow integer values.
        if (!_names.TryFormat(value, out string text) && !_allowIntegerValues)
            throw SerializationThrowHelper.EnumValueNotWritable(text, typeof(T));

        writer.WriteString(text);
    }

    /// <inheritdoc />
    bool IEnumKeyConverter<T>.TryReadKey(string text, out T value) =>
        _keys.TryReadKey(text, out value);

    /// <inheritdoc />
    string IEnumKeyConverter<T>.WriteKey(T value) =>
        _keys.WriteKey(value);
}
