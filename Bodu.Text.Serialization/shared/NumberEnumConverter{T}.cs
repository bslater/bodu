// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumberEnumConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

#if BENCODE
namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Produces a converter that serializes the enumeration <typeparamref name="TEnum" /> as a Bencode integer carrying its
/// underlying numeric value.
/// </summary>
/// <typeparam name="TEnum">The enumeration type the produced converter handles.</typeparam>
/// <remarks>
/// Reference the factory from a <see cref="ConverterAttribute" /> on a member, property, or the enumeration itself, or
/// register it on <see cref="BencodeSerializerOptions.Converters" />. It exposes a public parameterless constructor so
/// it can be used through the converter attribute. On read an integer outside the range of the underlying type of
/// <typeparamref name="TEnum" /> throws <see cref="BencodeSerializationException" /> rather than being narrowed into
/// that type; an enumeration based on <see cref="ulong" /> reads and writes integers up to
/// <see cref="ulong.MaxValue" />.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class WorkItem
/// {
///     [Converter(typeof(BencodeNumberEnumConverter<Priority>))]
///     public Priority Priority { get; set; }
/// }
///
/// // Priority.High (underlying value 2) serializes as the integer i2e.
///]]>
/// </code>
/// </example>
public sealed class BencodeNumberEnumConverter<TEnum>
    : BencodeConverterFactory
    where TEnum : struct, Enum
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeNumberEnumConverter{TEnum}" /> class.
    /// </summary>
    public BencodeNumberEnumConverter()
    {
    }
#elif TOML
namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Produces a converter that serializes the enumeration <typeparamref name="TEnum" /> as a TOML integer carrying its
/// underlying numeric value.
/// </summary>
/// <typeparam name="TEnum">The enumeration type the produced converter handles.</typeparam>
/// <remarks>
/// Reference the factory from a <see cref="ConverterAttribute" /> on a member, property, or the enumeration itself, or
/// register it on <see cref="TomlSerializerOptions.Converters" />. It exposes a public parameterless constructor so it
/// can be used through the converter attribute. On read an integer outside the range of the underlying type of
/// <typeparamref name="TEnum" /> throws <see cref="TomlSerializationException" /> rather than being narrowed into that
/// type. A TOML integer is a signed 64-bit value, so an enumeration based on <see cref="ulong" /> reads integers up to
/// <see cref="long.MaxValue" />, and writing a larger value throws <see cref="TomlSerializationException" />, as
/// writing such a <see cref="ulong" /> member does.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class WorkItem
/// {
///     [Converter(typeof(TomlNumberEnumConverter<Priority>))]
///     public Priority Priority { get; set; }
/// }
///
/// // Priority.High (underlying value 2) serializes as: Priority = 2
///]]>
/// </code>
/// </example>
public sealed class TomlNumberEnumConverter<TEnum>
    : TomlConverterFactory
    where TEnum : struct, Enum
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TomlNumberEnumConverter{TEnum}" /> class.
    /// </summary>
    public TomlNumberEnumConverter()
    {
    }
#elif YAML
namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Produces a converter that serializes the enumeration <typeparamref name="TEnum" /> as a YAML integer scalar carrying
/// its underlying numeric value.
/// </summary>
/// <typeparam name="TEnum">The enumeration type the produced converter handles.</typeparam>
/// <remarks>
/// Reference the factory from a <see cref="ConverterAttribute" /> on a member, property, or the enumeration itself, or
/// register it on <see cref="Bodu.Text.Yaml.YamlSerializerOptions.Converters" />. It exposes a public parameterless
/// constructor so it can be used through the converter attribute. The produced converter writes the numeric value
/// regardless of <see cref="Bodu.Text.Yaml.YamlSerializerOptions.WriteEnumsAsStrings" />. On read an integer scalar
/// outside the range of the underlying type of <typeparamref name="TEnum" /> throws
/// <see cref="Bodu.Text.Yaml.YamlSerializationException" /> rather than being narrowed into that type. A value of an
/// enumeration based on <see cref="ulong" /> above <see cref="long.MaxValue" /> is written as its plain invariant text,
/// as a <see cref="ulong" /> member is, and YAML resolves such a plain integer as a string, which the converter reads
/// back, up to <see cref="ulong.MaxValue" />, as a <see cref="ulong" /> member reads it; a quoted or tagged string is
/// rejected.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class WorkItem
/// {
///     [Converter(typeof(YamlNumberEnumConverter<Priority>))]
///     public Priority Priority { get; set; }
/// }
///
/// // Priority.High (underlying value 2) serializes as: Priority: 2
///]]>
/// </code>
/// </example>
public sealed class YamlNumberEnumConverter<TEnum>
    : YamlConverterFactory
    where TEnum : struct, Enum
{
    /// <summary>
    /// Initializes a new instance of the <see cref="YamlNumberEnumConverter{TEnum}" /> class.
    /// </summary>
    public YamlNumberEnumConverter()
    {
    }
#endif

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert == typeof(TEnum);

    /// <inheritdoc />
    public override FormatConverter CreateConverter(Type typeToConvert, FormatOptions options) =>
        new Converters.EnumNumberConverter<TEnum>();
}
