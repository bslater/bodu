// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamTests.CanWrite.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStreamTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundStream.CanWrite" /> reports <see langword="false" /> for a writable cursor once
    /// the file that owns it has been disposed, while the cursor can still be read.
    /// </summary>
    [TestMethod]
    public void CanWrite_WhenOwningFileIsDisposed_ShouldReturnFalse()
    {
        using CompoundStream cursor = OpenCursorOverDisposedFile();

        Assert.IsFalse(cursor.CanWrite);
        Assert.IsTrue(cursor.CanRead);
    }
}
