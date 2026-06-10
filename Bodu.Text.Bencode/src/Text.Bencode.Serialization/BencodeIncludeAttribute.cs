// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeIncludeAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Forces a property to participate in serialization even when its accessors are not public, allowing members such as
/// <c>{ get; private set; }</c> or <c>{ get; init; }</c> with a non-public setter to be read and written. Mirrors
/// <see cref="System.Text.Json.Serialization.JsonIncludeAttribute" />.
/// </summary>
/// <remarks>
/// Without this attribute a property is included only when it exposes a public getter, and is assigned only through a
/// public setter. When the attribute is present the serializer binds through the declared accessors regardless of their
/// visibility.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class BencodeIncludeAttribute
    : BencodeAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeIncludeAttribute" /> class.
    /// </summary>
    public BencodeIncludeAttribute()
    {
    }
}
