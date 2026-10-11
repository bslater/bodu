// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.ValuePlacement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Identifies where a generator stores a property value that is not held inline in its record.
    /// </summary>
    private enum ValuePlacement
    {
        /// <summary>
        /// A heap item when the value is at most 3580 bytes, otherwise a subnode of the owning node (MS-PST 2.3.3.3).
        /// </summary>
        Auto = 0,

        /// <summary>
        /// A heap item, whatever the value's length.
        /// </summary>
        Heap,

        /// <summary>
        /// A subnode of the owning node, whatever the value's length.
        /// </summary>
        Subnode,

        /// <summary>
        /// No storage: the record's value reference is zero.
        /// </summary>
        Null,

        /// <summary>
        /// No storage: the record's value reference is the given dword.
        /// </summary>
        Raw,
    }
}
