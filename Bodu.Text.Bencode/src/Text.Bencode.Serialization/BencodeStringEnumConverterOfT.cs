// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeStringEnumConverterOfT.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Bencode.Serialization.Converters;

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Produces a converter that serializes the enumeration <typeparamref name="TEnum" /> as a Bencode byte string holding
/// its member name, applying an optional naming policy and honoring <see cref="BencodeStringEnumMemberNameAttribute" />
/// on individual members.
/// </summary>
/// <typeparam name="TEnum">The enumeration type the produced converter handles.</typeparam>
/// <remarks>
/// Unlike the non-generic <see cref="BencodeStringEnumConverter" />, this strongly typed factory can be referenced from
/// a <see cref="BencodeConverterAttribute" /> on a member, property, or the enumeration itself, because it exposes a
/// public parameterless constructor and applies to a single enumeration type.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// [BencodeConverter(typeof(BencodeStringEnumConverter<Status>))]
/// public enum Status
/// {
///     Active,
///     OnHold,
/// }
///
/// // Status.OnHold serializes as the byte string 6:OnHold.
///]]>
/// </code>
/// </example>
public sealed class BencodeStringEnumConverter<TEnum>
    : BencodeConverterFactory
    where TEnum : struct, Enum
{
    /// <summary>The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.</summary>
    private readonly BencodeNamingPolicy? _namingPolicy;

    /// <summary>Whether a Bencode integer is accepted as an enumeration value on read.</summary>
    private readonly bool _allowIntegerValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeStringEnumConverter{TEnum}" /> class that applies no naming
    /// policy and accepts integers on read.
    /// </summary>
    public BencodeStringEnumConverter()
        : this(namingPolicy: null, allowIntegerValues: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeStringEnumConverter{TEnum}" /> class with the specified
    /// naming policy and integer-handling behavior.
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
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert == typeof(TEnum);

    /// <inheritdoc />
    public override BencodeConverter CreateConverter(Type typeToConvert, BencodeSerializerOptions options) =>
        new EnumConverter<TEnum>(_namingPolicy, _allowIntegerValues);
}
