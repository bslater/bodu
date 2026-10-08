// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReleaseNoteOptionsTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Corpus;

public sealed partial class ReleaseNoteOptionsTests
{
    /// <summary>
    /// Verifies that parsing a well-formed field returns its options.
    /// </summary>
    [TestMethod]
    public void Parse_WhenFieldIsWellFormed_ShouldReturnTheOptions()
    {
        IReadOnlyList<KeyValuePair<string, string>> options = ReleaseNoteOptions.Parse(@"Delimiter=\t");

        Assert.AreEqual(1, options.Count);
        Assert.AreEqual("Delimiter", options[0].Key);
        Assert.AreEqual("\t", options[0].Value);
    }

    /// <summary>
    /// Verifies that parsing a malformed field throws <see cref="FormatException" /> naming the problem.
    /// </summary>
    [TestMethod]
    public void Parse_WhenFieldIsMalformed_ShouldThrowFormatException()
    {
        var ex = Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = ReleaseNoteOptions.Parse("MaxDepth=1;MaxDepth=2");
        });

        Assert.IsTrue(ex.Message.Contains("MaxDepth", StringComparison.Ordinal), ex.Message);
    }
}
