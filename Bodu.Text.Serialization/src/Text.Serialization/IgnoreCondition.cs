// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IgnoreCondition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Specifies the condition under which a member is excluded from serialization, whether through
/// <see cref="IgnoreAttribute" /> on the member or through the serializer-wide default ignore condition.
/// </summary>
public enum IgnoreCondition
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
