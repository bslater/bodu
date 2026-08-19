// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTcColumn.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Represents one parsed <c>TCOLDESC</c>: a table-context column's tag and its cell geometry within a row.
/// </summary>
/// <param name="Tag">The column tag: the property identifier in the high 16 bits, the wire type in the low 16.</param>
/// <param name="DataOffset">The cell's offset within a row (<c>ibData</c>).</param>
/// <param name="DataSize">The cell's width in bytes (<c>cbData</c>).</param>
/// <param name="ExistenceBit">The cell's index in the row's existence bitmap (<c>iBit</c>).</param>
internal readonly record struct PstTcColumn(
    uint Tag,
    ushort DataOffset,
    byte DataSize,
    byte ExistenceBit)
{
    /// <summary>
    /// Gets the column's 16-bit property identifier.
    /// </summary>
    /// <value>The property identifier (the tag's high 16 bits).</value>
    internal ushort PropertyId => (ushort)(Tag >> 16);

    /// <summary>
    /// Gets the column's 16-bit wire type code.
    /// </summary>
    /// <value>The wire type (the tag's low 16 bits).</value>
    internal ushort WireType => (ushort)Tag;
}
