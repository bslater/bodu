// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatIgnoreCondition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Specifies when a member annotated with <see cref="FormatIgnoreAttribute" /> is excluded from serialization.
/// </summary>
public enum FormatIgnoreCondition
{
    /// <summary>
    /// The member is always ignored, for both reading and writing. This is the default.
    /// </summary>
    Always,

    /// <summary>
    /// The member is ignored when writing only if its value is <see langword="null" />.
    /// </summary>
    WhenWritingNull,

    /// <summary>
    /// The member is ignored when writing only if its value equals the default value of its type.
    /// </summary>
    WhenWritingDefault,
}
