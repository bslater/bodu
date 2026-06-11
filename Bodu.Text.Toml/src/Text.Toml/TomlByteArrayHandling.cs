// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlByteArrayHandling.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Toml;

/// <summary>
/// Specifies how a <see cref="byte" /> array is represented in TOML, which has no native binary type.
/// </summary>
/// <remarks>
/// The setting controls only how a byte array is written; on read the serializer accepts either form, so a document
/// produced under one setting can be read under either.
/// </remarks>
public enum TomlByteArrayHandling
{
    /// <summary>
    /// The byte array is written as a TOML array of integers, one integer per byte.
    /// </summary>
    IntegerArray = 0,

    /// <summary>
    /// The byte array is written as a Base64-encoded TOML basic string.
    /// </summary>
    Base64String = 1,
}
