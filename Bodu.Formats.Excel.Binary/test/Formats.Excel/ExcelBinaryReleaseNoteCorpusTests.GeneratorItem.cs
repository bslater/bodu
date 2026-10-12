// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReleaseNoteCorpusTests.GeneratorItem.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public sealed partial class ExcelBinaryReleaseNoteCorpusTests
{
    /// <summary>
    /// Represents one record constructor of a generator body, such as <c>NUMBER(0,0,1.5)</c>.
    /// </summary>
    /// <param name="Name">The constructor name, in upper case.</param>
    /// <param name="Arguments">The arguments as written, trimmed of surrounding white space.</param>
    private sealed record GeneratorItem(string Name, IReadOnlyList<string> Arguments);
}
