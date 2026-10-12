// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.TextArgument.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents a text argument of a generator: <c>'...'</c>, <c>w'...'</c> or <c>cpN'...'</c>.
    /// </summary>
    /// <param name="Text">The text, its catalogue escapes decoded.</param>
    /// <param name="Wide">Whether a BIFF8 string holding the text must use 16-bit characters (<c>w'...'</c>).</param>
    /// <param name="CodePage">
    /// The code page the text is encoded in, written in the BIFF5 layout even in a BIFF8 stream (<c>cpN'...'</c>); zero
    /// when the argument names none.
    /// </param>
    private sealed record TextArgument(string Text, bool Wide, int CodePage);
}
