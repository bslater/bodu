// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeStringEnumConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Serialization.Converters;

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Produces a converter that serializes any enumeration as a Bencode byte string holding its member name, applying an
/// optional naming policy and honoring <see cref="BencodeStringEnumMemberNameAttribute" /> on individual members.
/// Mirrors the non-generic <see cref="System.Text.Json.Serialization.JsonStringEnumConverter" />.
/// </summary>
/// <remarks>
/// Register the factory on <see cref="BencodeSerializerOptions.Converters" /> to apply it to every enumeration, or use
/// the generic <see cref="BencodeStringEnumConverter{TEnum}" /> with <see cref="BencodeConverterAttribute" /> to apply
/// it to a single enumeration. The two constructors match those of
/// <see cref="System.Text.Json.Serialization.JsonStringEnumConverter" />: a parameterless form that applies no naming
/// policy and accepts integers on read, and a form that takes an explicit naming policy and integer-handling flag.
/// </remarks>
public sealed class BencodeStringEnumConverter
    : BencodeConverterFactory
{
    /// <summary>
    /// The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.
    /// </summary>
    private readonly BencodeNamingPolicy? _namingPolicy;

    /// <summary>
    /// Whether a Bencode integer is accepted as an enumeration value on read.
    /// </summary>
    private readonly bool _allowIntegerValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeStringEnumConverter" /> class that applies no naming policy
    /// and accepts integers on read.
    /// </summary>
    public BencodeStringEnumConverter()
        : this(namingPolicy: null, allowIntegerValues: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeStringEnumConverter" /> class with the specified naming
    /// policy and integer-handling behavior.
    /// </summary>
    /// <param name="namingPolicy">
    /// The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.
    /// </param>
    /// <param name="allowIntegerValues">Whether a Bencode integer is accepted as an enumeration value on read.</param>
    public BencodeStringEnumConverter(BencodeNamingPolicy? namingPolicy, bool allowIntegerValues)
    {
        _namingPolicy = namingPolicy;
        _allowIntegerValues = allowIntegerValues;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return typeToConvert.IsEnum;
    }

    /// <inheritdoc />
    public override BencodeConverter CreateConverter(Type typeToConvert, BencodeSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type converterType = typeof(EnumConverter<>).MakeGenericType(typeToConvert);
        return (BencodeConverter)Activator.CreateInstance(converterType, new object?[] { _namingPolicy, _allowIntegerValues }) !;
    }
}
