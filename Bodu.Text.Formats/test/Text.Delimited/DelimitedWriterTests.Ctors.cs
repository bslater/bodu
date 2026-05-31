// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedWriterTests.Ctors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

public sealed partial class DelimitedWriterTests
{
    /// <summary>
    /// Verifies that constructing a <see cref="DelimitedWriter" /> with a <see langword="null" />
    /// <see cref="TextWriter" /> throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenWriterIsNull_ShouldThrowExactly()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new DelimitedWriter(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedWriter.RowsWritten" /> is zero immediately after construction.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenConstructed_ShouldHaveZeroRowsWritten()
    {
        using DelimitedWriter writer = new(TextWriter.Null);
        Assert.AreEqual(0, writer.RowsWritten);
    }
}
