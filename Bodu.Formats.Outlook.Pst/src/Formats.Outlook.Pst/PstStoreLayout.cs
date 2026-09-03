// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstStoreLayout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook.Pst;

/// <summary>
/// Encodes the MS-PST messaging layout conventions: how a folder's table node identifiers derive from the folder's
/// identifier, and the stored-subject prefix marker.
/// </summary>
internal static class PstStoreLayout
{
    /// <summary>
    /// Composes the hierarchy-table node identifier of a folder.
    /// </summary>
    /// <param name="folderId">The folder node identifier.</param>
    /// <returns>The identifier of the folder's hierarchy table.</returns>
    internal static PstNodeId HierarchyTableOf(PstNodeId folderId) =>
        new(PstNodeType.HierarchyTable, folderId.Index);

    /// <summary>
    /// Composes the contents-table node identifier of a folder.
    /// </summary>
    /// <param name="folderId">The folder node identifier.</param>
    /// <returns>The identifier of the folder's contents table.</returns>
    internal static PstNodeId ContentsTableOf(PstNodeId folderId) =>
        new(PstNodeType.ContentsTable, folderId.Index);

    /// <summary>
    /// Composes the associated-contents (folder-associated-information) table node identifier of a folder.
    /// </summary>
    /// <param name="folderId">The folder node identifier.</param>
    /// <returns>The identifier of the folder's associated-contents table.</returns>
    internal static PstNodeId AssociatedContentsTableOf(PstNodeId folderId) =>
        new(PstNodeType.AssociatedContentsTable, folderId.Index);
}
