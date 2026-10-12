// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookOmittedValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Describes a property value the mail-store reader omitted from an object it decoded, as reported to
/// <see cref="OutlookMailStoreReaderOptions.ValueOmittedCallback" />.
/// </summary>
/// <remarks>
/// An omitted value is absent from the object's properties, so a report is how a caller tells an object salvaged from a
/// damaged store from a clean one. <see cref="NodeId" /> and <see cref="RowId" /> locate the value in the container;
/// <see cref="PropertyId" /> names the property it would have supplied.
/// </remarks>
public sealed class OutlookOmittedValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookOmittedValue" /> class.
    /// </summary>
    /// <param name="nodeId">The node whose property context or table held the value.</param>
    /// <param name="rowId">The row that held the value, or <see langword="null" /> for a property context.</param>
    /// <param name="propertyId">The 16-bit identifier of the omitted property.</param>
    /// <param name="reason">Why the value was omitted.</param>
    internal OutlookOmittedValue(PstNodeId nodeId, uint? rowId, ushort propertyId, OutlookOmittedValueReason reason)
    {
        NodeId = nodeId;
        RowId = rowId;
        PropertyId = propertyId;
        Reason = reason;
    }

    /// <summary>
    /// Gets the node whose property context or table held the value: the folder, message, attachment or store node, the
    /// name-to-id map node, or a message's recipient table.
    /// </summary>
    /// <value>The node identifier.</value>
    public PstNodeId NodeId { get; }

    /// <summary>
    /// Gets the identifier of the table row that held the value.
    /// </summary>
    /// <value>
    /// The row identifier for a cell of a recipient row; <see langword="null" /> for a value of a property context.
    /// </value>
    public uint? RowId { get; }

    /// <summary>
    /// Gets the identifier of the property the omitted value would have supplied.
    /// </summary>
    /// <value>The 16-bit property identifier.</value>
    public ushort PropertyId { get; }

    /// <summary>
    /// Gets why the value was omitted.
    /// </summary>
    /// <value>The reason.</value>
    public OutlookOmittedValueReason Reason { get; }

    /// <summary>
    /// Returns a textual form of the report for diagnostics.
    /// </summary>
    /// <returns>The node, the row when there is one, the property identifier, and the reason.</returns>
    public override string ToString() =>
        RowId is uint rowId
            ? string.Create(CultureInfo.InvariantCulture, $"{NodeId} row 0x{rowId:X8} property 0x{PropertyId:X4}: {Reason}")
            : string.Create(CultureInfo.InvariantCulture, $"{NodeId} property 0x{PropertyId:X4}: {Reason}");
}
