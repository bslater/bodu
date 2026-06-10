// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Serves as the base class for the attributes that customize how a type or member is mapped to and from Bencode by the
/// serializer. Mirrors the role of <see cref="System.Text.Json.Serialization.JsonAttribute" />.
/// </summary>
/// <remarks>
/// This base type lets the Bencode serialization attributes be discovered and reasoned about as a single family,
/// matching the way <see cref="System.Text.Json.Serialization.JsonAttribute" /> groups the <c>System.Text.Json</c>
/// attributes.
/// </remarks>
public abstract class BencodeAttribute
    : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeAttribute" /> class.
    /// </summary>
    protected BencodeAttribute()
    {
    }
}
