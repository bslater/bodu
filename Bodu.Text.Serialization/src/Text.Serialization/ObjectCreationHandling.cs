// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObjectCreationHandling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Specifies whether the serializer replaces a member's value with a freshly created instance or populates the value
/// already held by the member during deserialization.
/// </summary>
/// <remarks>
/// <see cref="Populate" /> reads a member into the value it already holds: the entries read for a collection or
/// dictionary are added to it, and the members read for an object are set on it, so a get-only collection or object
/// property initialized in the type can round-trip. A member of a value type is populated on a copy that its setter
/// stores back. The serializer falls back to replacing the value when there is nothing it can populate: the existing
/// value is <see langword="null" />, the collection cannot grow (an array, or a read-only or fixed-size collection or
/// dictionary), the object's type is built through a parameterized constructor, or a value-type member has no setter. A
/// member that cannot be replaced either, because it has no setter, keeps its value.
/// </remarks>
public enum ObjectCreationHandling
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
