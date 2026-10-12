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
/// <para>
/// Register the factory on <see cref="YamlSerializerOptions.Converters" /> to apply it to every enumeration, or use the
/// generic <see cref="YamlStringEnumConverter{TEnum}" /> with <see cref="ConverterAttribute" /> to apply it to a single
/// enumeration. Two constructors are provided: a parameterless form that applies no naming policy and accepts integers
/// on read, and a form that takes an explicit naming policy and integer-handling flag. The produced converter writes
/// the member name regardless of <see cref="YamlSerializerOptions.WriteEnumsAsStrings" />. An integer accepted on read
/// must lie within the range of the enumeration's underlying type; one outside it throws
/// <see cref="YamlSerializationException" /> rather than being narrowed into that type.
/// </para>
/// <para>
/// A converter that does not allow integers throws <see cref="YamlSerializationException" /> on an integer scalar, and
/// at any other scalar that is not a member's wire name and whose text has an integer in any of its comma-separated
/// parts: a quoted <c>"2"</c> or <c>" +2 "</c>, <c>!!str 2</c>, a plain integer beyond the 64-bit range, which YAML
/// resolves as a string, an integral float such as <c>2.0</c>, or a list such as <c>Read, 2</c>. Each is rejected
/// rather than bound as the integer, as System.Text.Json's <c>JsonStringEnumConverter</c> rejects a numeric string from
/// .NET 9. The wire names are matched first, so a member named <c>1</c> through
/// <see cref="StringEnumMemberNameAttribute" /> still reads from a string scalar. Such a converter also refuses to
/// write a value that is neither a member nor a combination of defined flags, such as an undefined value or a flags
/// value with an undefined bit, and throws <see cref="YamlSerializationException" /> rather than writing the decimal
/// text it would reject on read.
/// </para>
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

    /// <summary>Whether integer values are allowed: read from a YAML integer scalar or a scalar whose text holds one, and written as the decimal text of a value with no name.</summary>
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
    /// <param name="allowIntegerValues">
    /// Whether integer values are allowed: read from a YAML integer scalar or a scalar whose text holds one, and
    /// written as the decimal text of a value that is neither a member nor a combination of flags.
    /// </param>
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
