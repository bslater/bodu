// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableColumn.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.IO.Pst;

/// <summary>
/// Represents one column of a table context: its 16-bit property identifier, its raw wire type code, and its cell
/// width within a row.
/// </summary>
public readonly struct PstTableColumn
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PstTableColumn" /> struct.
    /// </summary>
    /// <param name="propertyId">The 16-bit property identifier.</param>
    /// <param name="wireType">The 16-bit wire type code.</param>
    /// <param name="width">The cell width in bytes.</param>
    internal PstTableColumn(ushort propertyId, ushort wireType, int width)
    {
        PropertyId = propertyId;
        WireType = wireType;
        Width = width;
    }

    /// <summary>
    /// Gets the column's 16-bit property identifier.
    /// </summary>
    /// <value>The property identifier.</value>
    public ushort PropertyId { get; }

    /// <summary>
    /// Gets the column's raw 16-bit property wire type code.
    /// </summary>
    /// <value>The MS-OXCDATA type code, unchanged from the file.</value>
    public ushort WireType { get; }

    /// <summary>
    /// Gets the column's cell width within a row.
    /// </summary>
    /// <value>The width in bytes; a variable-size column's cell holds a four-byte value reference.</value>
    public int Width { get; }

    /// <summary>
    /// Returns a textual form of the column for diagnostics.
    /// </summary>
    /// <returns>The property identifier, wire type, and width.</returns>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"0x{PropertyId:X4} (0x{WireType:X4}, {Width} bytes)");
}
