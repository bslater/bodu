// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNamedPropertyMapTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst;
using Bodu.IO.Pst.Internal;

namespace Bodu.Formats.Outlook.Pst;

/// <summary>
/// Verifies the behavior of <see cref="PstNamedPropertyMap" />, the name-to-id map parser, over synthetic map nodes
/// with byte-exact control of the three streams.
/// </summary>
[TestClass]
public partial class PstNamedPropertyMapTests
{
    /// <summary>A custom property-set GUID stored in the GUID stream (GUID index 3).</summary>
    internal static readonly Guid CustomSet = new("11111111-2222-3333-4444-555555555555");

    /// <summary>
    /// Builds a name-to-id map node from raw streams and parses it.
    /// </summary>
    /// <param name="guids">The GUID-stream payload.</param>
    /// <param name="entries">The entry-stream payload.</param>
    /// <param name="strings">The string-stream payload.</param>
    /// <param name="strict">Whether to parse under strict validation.</param>
    /// <returns>The parsed mapping.</returns>
    internal static PstNamedPropertyMap Load(byte[] guids, byte[] entries, byte[] strings, bool strict)
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();
        uint guidsHid = ltp.AddItem(guids);
        uint entriesHid = ltp.AddItem(entries);
        uint stringsHid = ltp.AddItem(strings);

        _ = ltp.AddPropertyContext(
            (0x0001, 0x0003, 251),
            (0x0002, 0x0102, guidsHid),
            (0x0003, 0x0102, entriesHid),
            (0x0004, 0x0102, stringsHid));
        ltp.AddHeapNode(builder, PstNodeId.NameToIdMap.Value);

        using PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions());
        return PstNamedPropertyMap.Load(file.GetNode(PstNodeId.NameToIdMap).ReadPropertyContext(), strict);
    }

    /// <summary>
    /// Composes one 8-byte entry of the entry stream.
    /// </summary>
    /// <param name="idOrOffset">The numeric name identifier or string-stream offset.</param>
    /// <param name="guidIndex">The GUID index.</param>
    /// <param name="isString">Whether the entry names a string property.</param>
    /// <param name="propertyIndex">The property index.</param>
    /// <returns>The entry bytes.</returns>
    internal static byte[] Entry(uint idOrOffset, int guidIndex, bool isString, ushort propertyIndex)
    {
        var entry = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(entry, idOrOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(entry.AsSpan(4), (ushort)((guidIndex << 1) | (isString ? 1 : 0)));
        BinaryPrimitives.WriteUInt16LittleEndian(entry.AsSpan(6), propertyIndex);
        return entry;
    }
}
