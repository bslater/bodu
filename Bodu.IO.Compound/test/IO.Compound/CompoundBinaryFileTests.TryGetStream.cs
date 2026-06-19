// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundBinaryFileTests.TryGetStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundBinaryFileTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundBinaryFile.TryGetStream(string, out CompoundStream)" /> returns
    /// <see langword="false" /> for a stream that does not exist.
    /// </summary>
    [TestMethod]
    public void TryGetStream_WhenStreamMissing_ShouldReturnFalse()
    {
        using CompoundBinaryFile file = OpenSample();

        bool found = file.TryGetStream("DoesNotExist", out CompoundStream? stream);

        Assert.IsFalse(found);
        Assert.IsNull(stream);
    }
}
