// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeIncludeAttribute.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Forces a member to participate in serialization: a property's non-public accessors are bound (allowing members such
/// as <c>{ get; private set; }</c> or <c>{ get; init; }</c> with a non-public setter to be read and written), and a
/// public field is surfaced even when <see cref="BencodeSerializerOptions.IncludeFields" /> is disabled.
/// </summary>
/// <remarks>
/// Without this attribute a property is included only when it exposes a public getter, and is assigned only through a
/// public setter; when the attribute is present the serializer binds through the declared accessors regardless of their
/// visibility. Public fields participate only when this attribute is applied or
/// <see cref="BencodeSerializerOptions.IncludeFields" /> is enabled; non-public fields are never surfaced.
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
