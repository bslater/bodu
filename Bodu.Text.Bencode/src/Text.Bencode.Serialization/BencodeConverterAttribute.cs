// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeConverterAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Specifies the <see cref="BencodeConverter" /> to use for a property, field, or type, taking precedence over the
/// converters configured on the serializer options.
/// </summary>
/// <remarks>
/// The referenced type must derive from <see cref="BencodeConverter" /> and declare a public parameterless constructor.
/// Applied to a member, the converter governs that member only; applied to a type, it governs every use of the type.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// public sealed class Package
/// {
///     [BencodeConverter(typeof(VersionConverter))]
///     public Version Version { get; set; } = new(1, 2, 3);
/// }
///
/// // VersionConverter governs the member, writing the byte string 5:1.2.3.
///]]>
/// </code>
/// </example>
[AttributeUsage(
    AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum,
    AllowMultiple = false,
    Inherited = true)]
public sealed class BencodeConverterAttribute
    : BencodeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeConverterAttribute" /> class.
    /// </summary>
    /// <param name="converterType">The converter type to use.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="converterType" /> is <see langword="null" />.
    /// </exception>
    public BencodeConverterAttribute(Type converterType)
    {
        ThrowHelper.ThrowIfNull(converterType);

        ConverterType = converterType;
    }

    /// <summary>
    /// Gets the converter type to use.
    /// </summary>
    /// <value>The converter type.</value>
    public Type ConverterType { get; }
}
