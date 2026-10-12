// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstReleaseNoteCorpusTests.GeneratedFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public sealed partial class PstReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents the file a generator wrote, which the pipeline's transforms then patch in place.
    /// </summary>
    /// <param name="Bytes">The file bytes.</param>
    /// <param name="Format">The format the file was written in, which fixes the offsets the transforms patch.</param>
    private sealed record GeneratedFile(byte[] Bytes, PstFileFormat Format);
}
