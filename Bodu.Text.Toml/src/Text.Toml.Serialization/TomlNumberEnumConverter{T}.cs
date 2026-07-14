// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlNumberEnumConverter{T}.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using Bodu.Text.Toml.Serialization.Converters;

namespace Bodu.Text.Toml.Serialization;

/// <summary>
/// Produces a converter that serializes the enumeration <typeparamref name="TEnum" /> as a TOML integer carrying its
/// underlying numeric value.
/// </summary>
/// <typeparam name="TEnum">The enumeration type the produced converter handles.</typeparam>
/// <remarks>
/// Reference the factory from a <see cref="ConverterAttribute" /> on a member, property, or the enumeration itself, or
/// register it on <see cref="TomlSerializerOptions.Converters" />. It exposes a public parameterless constructor so it
/// can be used through the converter attribute.
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

    /// <inheritdoc />
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert == typeof(TEnum);

    /// <inheritdoc />
    public override TomlConverter CreateConverter(Type typeToConvert, TomlSerializerOptions options) =>
        new EnumNumberConverter<TEnum>();
}
