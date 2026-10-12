// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlStringEnumConverter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using Bodu.Text.Toml.Serialization.Converters;

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Produces a converter that serializes any enumeration as a TOML string holding its member name, applying an optional
/// naming policy and honoring <see cref="StringEnumMemberNameAttribute" /> on individual members.
/// </summary>
/// <remarks>
/// <para>
/// Register the factory on <see cref="TomlSerializerOptions.Converters" /> to apply it to every enumeration, or use the
/// generic <see cref="TomlStringEnumConverter{TEnum}" /> with <see cref="ConverterAttribute" /> to apply it to a single
/// enumeration. Two constructors are provided: a parameterless form that applies no naming policy and accepts integers
/// on read, and a form that takes an explicit naming policy and integer-handling flag. An integer accepted on read must
/// lie within the range of the enumeration's underlying type; one outside it throws
/// <see cref="TomlSerializationException" /> rather than being narrowed into that type.
/// </para>
/// <para>
/// Names are matched as System.Text.Json's <c>JsonStringEnumConverter</c> matches them. A member's wire name, from
/// <see cref="StringEnumMemberNameAttribute" /> or the naming policy, binds only in its own case; the CLR name of a
/// member the attribute does not rename also binds, in any case, while a renamed member's CLR name does not. A
/// combination of flags is written as the comma-separated wire names of its flags, in the order
/// <see cref="Enum.ToString()" /> lists them, and is read from a list of names in any order, with a trailing comma
/// tolerated; an integer binds, where integers are allowed, only as the whole text.
/// </para>
/// <para>
/// A converter that does not allow integers throws <see cref="TomlSerializationException" /> on a TOML integer, and at
/// a string that holds one: a string that is not a member's wire name and has an integer in any of its comma-separated
/// parts, such as <c>"2"</c>, <c>" +2 "</c> or <c>"Read, 2"</c>, is rejected rather than bound as the integer, as
/// System.Text.Json's <c>JsonStringEnumConverter</c> rejects such a string from .NET 9. The wire names are matched
/// first, so a member named <c>1</c> through <see cref="StringEnumMemberNameAttribute" /> still reads. Such a converter
/// also refuses to write a value that is neither a member nor a combination of defined flags, such as an undefined
/// value or a flags value with an undefined bit, and throws <see cref="TomlSerializationException" /> rather than
/// writing the decimal text it would reject on read.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// var options = new TomlSerializerOptions();
/// options.Converters.Add(new TomlStringEnumConverter(NamingPolicy.SnakeCaseLower, allowIntegerValues: false));
///
/// // Status.OnHold now serializes as the TOML string "on_hold".
///]]>
/// </code>
/// </example>
public sealed class TomlStringEnumConverter
    : TomlConverterFactory
{
    /// <summary>The naming policy applied to member names, or <see langword="null" /> to use member names unchanged.</summary>
    private readonly NamingPolicy? _namingPolicy;

    /// <summary>Whether integer values are allowed: read from a TOML integer or a string that holds one, and written as the decimal text of a value with no name.</summary>
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
    /// <param name="allowIntegerValues">
    /// Whether integer values are allowed: read from a TOML integer or a string that holds one, and written as the
    /// decimal text of a value that is neither a member nor a combination of flags.
    /// </param>
    public TomlStringEnumConverter(NamingPolicy? namingPolicy, bool allowIntegerValues)
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
        return (TomlConverter)Activator.CreateInstance(converterType, new object?[] { _namingPolicy, _allowIntegerValues })!;
    }
}
