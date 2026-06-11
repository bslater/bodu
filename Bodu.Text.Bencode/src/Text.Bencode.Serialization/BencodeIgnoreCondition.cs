// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeIgnoreCondition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode.Serialization;

/// <summary>
/// Specifies the condition under which a member is excluded from serialization, whether through
/// <see cref="BencodeIgnoreAttribute" /> on the member or through the serializer-wide default ignore condition. Mirrors
/// <see cref="System.Text.Json.Serialization.JsonIgnoreCondition" />.
/// </summary>
public enum BencodeIgnoreCondition
{
    /// <summary>
    /// Property is never ignored.
    /// </summary>
    Never = 0,

    /// <summary>
    /// Property is always ignored.
    /// </summary>
    Always = 1,

    /// <summary>
    /// Ignored on write when it equals the default for its type.
    /// </summary>
    WhenWritingDefault = 2,

    /// <summary>
    /// Ignored on write when null.
    /// </summary>
    WhenWritingNull = 3,
}
