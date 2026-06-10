// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeRequiredAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Marks a property or field as required, so deserialization fails when the corresponding key is absent from the input.
/// Mirrors <see cref="System.Text.Json.Serialization.JsonRequiredAttribute" />.
/// </summary>
/// <remarks>
/// Applying this attribute has the same effect as declaring the member with the <see langword="required" /> keyword:
/// the member must appear in the Bencode dictionary being read, otherwise a
/// <see cref="BencodeSerializationException" /> is thrown.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class BencodeRequiredAttribute
    : BencodeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeRequiredAttribute" /> class.
    /// </summary>
    public BencodeRequiredAttribute()
    {
    }
}
