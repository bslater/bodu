// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.GetSingle.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies <see cref="Utf8TomlReader.GetSingle" />, including a float literal that does not narrow to
/// <see cref="float" />.
/// </summary>
public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.GetSingle" /> throws <see cref="FormatException" /> for a finite float
    /// literal outside the <see cref="float" /> range, rather than returning an infinity.
    /// </summary>
    /// <param name="literal">The float literal outside the range.</param>
    [TestMethod]
    [DataRow("3.4028236e38", DisplayName = "just past float.MaxValue")]
    [DataRow("-1e300", DisplayName = "far past float.MinValue")]
    public void GetSingle_WhenLiteralIsOutsideSingleRange_ShouldThrowFormatException(string literal)
    {
        _ = Assert.ThrowsExactly<FormatException>(() =>
        {
            Utf8TomlReader reader = Create($"f = {literal}\n");
            Advance(ref reader, 2);
            _ = reader.GetSingle();
        });
    }
}
