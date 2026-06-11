// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNumberEnumConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Serialization.Converters;

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Produces a converter that serializes the enumeration <typeparamref name="TEnum" /> as a Bencode integer carrying its
/// underlying numeric value.
/// </summary>
/// <typeparam name="TEnum">The enumeration type the produced converter handles.</typeparam>
/// <remarks>
/// Reference the factory from a <see cref="BencodeConverterAttribute" /> on a member, property, or the enumeration
/// itself, or register it on <see cref="BencodeSerializerOptions.Converters" />. It exposes a public parameterless
/// constructor so it can be used through the converter attribute.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class WorkItem
/// {
///     [BencodeConverter(typeof(BencodeNumberEnumConverter<Priority>))]
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

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert == typeof(TEnum);

    /// <inheritdoc />
    public override BencodeConverter CreateConverter(Type typeToConvert, BencodeSerializerOptions options) =>
        new EnumNumberConverter<TEnum>();
}
