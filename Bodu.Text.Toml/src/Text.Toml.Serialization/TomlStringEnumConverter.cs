// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlStringEnumConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Serialization.Converters;

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Produces a converter that serializes any enumeration as a TOML string holding its member name, applying an optional
/// naming policy and honoring <see cref="TomlStringEnumMemberNameAttribute" /> on individual members. Mirrors the
/// non-generic <see cref="System.Text.Json.Serialization.JsonStringEnumConverter" />.
/// </summary>
/// <remarks>
/// Register the factory on <see cref="TomlSerializerOptions.Converters" /> to apply it to every enumeration, or use the
/// generic <see cref="TomlStringEnumConverter{TEnum}" /> with <see cref="TomlConverterAttribute" /> to apply it to a
/// single enumeration. The two constructors match those of
/// <see cref="System.Text.Json.Serialization.JsonStringEnumConverter" />: a parameterless form that applies no naming
/// policy and accepts integers on read, and a form that takes an explicit naming policy and integer-handling flag.
/// </remarks>
public sealed class TomlStringEnumConverter
    : TomlConverterFactory
{
    /// <summary>
    /// The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.
    /// </summary>
    private readonly TomlNamingPolicy? _namingPolicy;

    /// <summary>
    /// Whether a TOML integer is accepted as an enumeration value on read.
    /// </summary>
    private readonly bool _allowIntegerValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlStringEnumConverter" /> class that applies no naming policy and
    /// accepts integers on read.
    /// </summary>
    public TomlStringEnumConverter()
        : this(namingPolicy: null, allowIntegerValues: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TomlStringEnumConverter" /> class with the specified naming policy
    /// and integer-handling behavior.
    /// </summary>
    /// <param name="namingPolicy">
    /// The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.
    /// </param>
    /// <param name="allowIntegerValues">Whether a TOML integer is accepted as an enumeration value on read.</param>
    public TomlStringEnumConverter(TomlNamingPolicy? namingPolicy, bool allowIntegerValues)
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
    public override TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type converterType = typeof(EnumConverter<>).MakeGenericType(typeToConvert);
        return (TomlConverter)Activator.CreateInstance(converterType, new object?[] { _namingPolicy, _allowIntegerValues }) !;
    }
}
