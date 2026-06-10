// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeObjectCreationHandling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Specifies whether the serializer replaces a member's value with a freshly created instance or populates the value
/// already held by the member during deserialization. Mirrors
/// <see cref="System.Text.Json.Serialization.JsonObjectCreationHandling" />.
/// </summary>
/// <remarks>
/// <see cref="Populate" /> applies to collection and dictionary members: the entries read from the input are added into
/// the existing instance, so a get-only collection property initialized in the type can round-trip. When the existing
/// value is <see langword="null" /> or the member is not a populatable collection, the serializer falls back to
/// replacing the value.
/// </remarks>
public enum BencodeObjectCreationHandling
{
    /// <summary>
    /// A new instance is created and assigned to the member.
    /// </summary>
    Replace = 0,

    /// <summary>
    /// The instance already held by the member is reused and populated with the read entries.
    /// </summary>
    Populate = 1,
}
