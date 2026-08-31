// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NodesAndProperties.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Samples.PstBasics.Scenarios;

/// <summary>
/// Demonstrates the container's two LTP views without any MAPI semantics: the message-store node's
/// property context (a bag of wire-typed values), and the root folder's hierarchy table (a table
/// context whose row identifiers are the child folders' node identifiers).
/// </summary>
public static class NodesAndProperties
{
    /// <summary>
    /// Dumps the store node's property bag and walks the root hierarchy table.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Raw nodes: property and table contexts ---");

        using PstFile file = PstFile.OpenRead(Program.SamplePath);

        // The message-store object (well-known NID 0x21): a property context of wire-typed values.
        // 0x001F is PT_UNICODE - the container exposes the type code, not the property's meaning.
        PstNode store = file.GetNode(PstNodeId.MessageStore);
        Console.WriteLine("message store (0x21) property context:");
        foreach (PstPropertyValue value in store.ReadPropertyContext())
        {
            string rendered = value.WireType == 0x001F ? $"\"{value.GetString()}\"" : $"{value.RawData.Length} bytes";
            Console.WriteLine($"  0x{value.PropertyId:X4} (wire 0x{value.WireType:X4}): {rendered}");
        }

        // The root folder's hierarchy table: same index as the root folder (0x122), hierarchy-table type bits.
        var hierarchyId = new PstNodeId(PstNodeType.HierarchyTable, PstNodeId.RootFolder.Index);
        if (file.TryGetNode(hierarchyId, out PstNode? table))
        {
            PstTableContext context = table.ReadTableContext();
            Console.WriteLine($"root hierarchy table: {context.RowCount} rows x {context.Columns.Count} columns");
            foreach (PstTableRow row in context.EnumerateRows())
            {
                var childId = new PstNodeId(row.RowId);
                Console.WriteLine($"  row 0x{row.RowId:X8} -> child {childId.Type}");
            }
        }

        Console.WriteLine();
    }
}
