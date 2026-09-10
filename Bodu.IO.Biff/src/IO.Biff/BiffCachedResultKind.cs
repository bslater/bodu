// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffCachedResultKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Identifies the kind of cached result a <c>FORMULA</c> record carries in its eight-byte result field.
/// </summary>
/// <remarks>
/// A result whose last two bytes are <c>0xFFFF</c> is not a number; its first byte then selects the kind, and a string
/// result is carried by the <c>STRING</c> record that follows the formula.
/// </remarks>
public enum BiffCachedResultKind
{
    /// <summary>
    /// The result is the IEEE 754 double stored in the field.
    /// </summary>
    Number = 0,

    /// <summary>
    /// The result is text, carried by the following <c>STRING</c> record.
    /// </summary>
    String = 1,

    /// <summary>
    /// The result is a boolean.
    /// </summary>
    Boolean = 2,

    /// <summary>
    /// The result is an error code.
    /// </summary>
    Error = 3,

    /// <summary>
    /// The result is an empty string.
    /// </summary>
    Empty = 4,
}
