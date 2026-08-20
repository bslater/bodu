// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlStringEnumConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using Bodu.Text.Yaml.Serialization.Converters;

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Produces a converter that serializes the enumeration <typeparamref name="TEnum" /> as a YAML string holding its
/// member name, applying an optional naming policy and honoring <see cref="StringEnumMemberNameAttribute" /> on
/// individual members.
/// </summary>
/// <typeparam name="TEnum">The enumeration type the produced converter handles.</typeparam>
/// <remarks>
/// Unlike the non-generic <see cref="YamlStringEnumConverter" />, this strongly typed factory can be referenced from a
/// <see cref="ConverterAttribute" /> on a member, property, or the enumeration itself, because it exposes a public
/// parameterless constructor and applies to a single enumeration type.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// [Converter(typeof(YamlStringEnumConverter<Status>))]
/// public enum Status
/// {
///     Active,
///     OnHold,
/// }
///
/// // Status.OnHold serializes as the YAML scalar OnHold.
///]]>
/// </code>
/// </example>
public sealed class YamlStringEnumConverter<TEnum>
    : YamlConverterFactory
    where TEnum : struct, Enum
{
    /// <summary>The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.</summary>
    private readonly NamingPolicy? _namingPolicy;

    /// <summary>Whether a YAML integer scalar is accepted as an enumeration value on read.</summary>
    private readonly bool _allowIntegerValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlStringEnumConverter{TEnum}" /> class that applies no naming
    /// policy and accepts integers on read.
    /// </summary>
    public YamlStringEnumConverter()
        : this(namingPolicy: null, allowIntegerValues: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlStringEnumConverter{TEnum}" /> class with the specified naming
    /// policy and integer-handling behavior.
    /// </summary>
    /// <param name="namingPolicy">
    /// The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.
    /// </param>
    /// <param name="allowIntegerValues">Whether a YAML integer scalar is accepted as an enumeration value on read.</param>
    public YamlStringEnumConverter(NamingPolicy? namingPolicy, bool allowIntegerValues)
    {
        _namingPolicy = namingPolicy;
        _allowIntegerValues = allowIntegerValues;
    }

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert == typeof(TEnum);

    /// <inheritdoc />
    public override YamlConverter CreateConverter(Type typeToConvert, YamlSerializerOptions options) =>
        new EnumConverter<TEnum>(_namingPolicy, _allowIntegerValues);
}
