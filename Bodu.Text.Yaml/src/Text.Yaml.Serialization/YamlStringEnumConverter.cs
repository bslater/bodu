// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlStringEnumConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using Bodu.Text.Yaml.Serialization.Converters;

namespace Bodu.Text.Yaml.Serialization;

/// <summary>
/// Produces a converter that serializes any enumeration as a YAML string holding its member name, applying an optional
/// naming policy and honoring <see cref="StringEnumMemberNameAttribute" /> on individual members.
/// </summary>
/// <remarks>
/// Register the factory on <see cref="YamlSerializerOptions.Converters" /> to apply it to every enumeration, or use the
/// generic <see cref="YamlStringEnumConverter{TEnum}" /> with <see cref="ConverterAttribute" /> to apply it to a single
/// enumeration. Two constructors are provided: a parameterless form that applies no naming policy and accepts integers
/// on read, and a form that takes an explicit naming policy and integer-handling flag. The produced converter writes
/// the member name regardless of <see cref="YamlSerializerOptions.WriteEnumsAsStrings" />.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var options = new YamlSerializerOptions();
/// options.Converters.Add(new YamlStringEnumConverter(NamingPolicy.SnakeCaseLower, allowIntegerValues: false));
///
/// // Status.OnHold now serializes as the YAML scalar on_hold.
///]]>
/// </code>
/// </example>
public sealed class YamlStringEnumConverter
    : YamlConverterFactory
{
    /// <summary>The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.</summary>
    private readonly NamingPolicy? _namingPolicy;

    /// <summary>Whether a YAML integer scalar is accepted as an enumeration value on read.</summary>
    private readonly bool _allowIntegerValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlStringEnumConverter" /> class that applies no naming policy and
    /// accepts integers on read.
    /// </summary>
    public YamlStringEnumConverter()
        : this(namingPolicy: null, allowIntegerValues: true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlStringEnumConverter" /> class with the specified naming policy
    /// and integer-handling behavior.
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
    public override bool CanConvert(Type typeToConvert)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);
        return typeToConvert.IsEnum;
    }

    /// <inheritdoc />
    public override YamlConverter CreateConverter(Type typeToConvert, YamlSerializerOptions options)
    {
        ThrowHelper.ThrowIfNull(typeToConvert);

        Type converterType = typeof(EnumConverter<>).MakeGenericType(typeToConvert);
        return (YamlConverter)Activator.CreateInstance(converterType, new object?[] { _namingPolicy, _allowIntegerValues })!;
    }
}
