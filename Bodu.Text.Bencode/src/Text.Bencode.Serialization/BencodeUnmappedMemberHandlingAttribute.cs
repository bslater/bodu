// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeUnmappedMemberHandlingAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Specifies, for the annotated type, how the serializer treats a dictionary key that maps to no member during
/// deserialization, overriding the serializer-wide <see cref="BencodeSerializerOptions.UnmappedMemberHandling" />.
/// </summary>
/// <remarks>
/// When a type carries this attribute, its <see cref="UnmappedMemberHandling" /> is used in place of the options-level
/// default for that type. A type with an extension-data member still captures unmapped keys into that member regardless
/// of this setting.
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// [BencodeUnmappedMemberHandling(BencodeUnmappedMemberHandling.Disallow)]
/// public sealed class StrictConfig
/// {
///     public int Port { get; set; }
/// }
///
/// // Input containing a key that maps to no member throws BencodeSerializationException.
///]]>
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class BencodeUnmappedMemberHandlingAttribute
    : BencodeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeUnmappedMemberHandlingAttribute" /> class.
    /// </summary>
    /// <param name="unmappedMemberHandling">The handling applied to unmapped keys for the annotated type.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="unmappedMemberHandling" /> is not a defined
    /// <see cref="BencodeUnmappedMemberHandling" /> value.
    /// </exception>
    public BencodeUnmappedMemberHandlingAttribute(BencodeUnmappedMemberHandling unmappedMemberHandling)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(unmappedMemberHandling);

        UnmappedMemberHandling = unmappedMemberHandling;
    }

    /// <summary>
    /// Gets the handling applied to unmapped keys for the annotated type.
    /// </summary>
    /// <value>The unmapped-member handling.</value>
    public BencodeUnmappedMemberHandling UnmappedMemberHandling { get; }
}
