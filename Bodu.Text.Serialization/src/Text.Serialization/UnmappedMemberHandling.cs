// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnmappedMemberHandling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Serialization;

/// <summary>
/// Specifies how the serializer treats a dictionary key that maps to no member of the target type during
/// deserialization.
/// </summary>
/// <remarks>
/// A type that declares an extension-data member captures unmapped keys into that member, which takes precedence over
/// this setting, so a key absorbed by extension data never triggers <see cref="Disallow" />.
/// </remarks>
public enum UnmappedMemberHandling
{
    /// <summary>
    /// An unmapped key is silently skipped.
    /// </summary>
    Skip = 0,

    /// <summary>
    /// An unmapped key causes deserialization to fail.
    /// </summary>
    Disallow = 1,
}
