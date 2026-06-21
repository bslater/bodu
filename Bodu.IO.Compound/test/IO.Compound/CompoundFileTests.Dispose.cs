// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that disposing a compound file twice does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        CompoundFile file = OpenSample();

        file.Dispose();
        file.Dispose();
    }

    /// <summary>
    /// Verifies that materializing a stream after the file has been disposed throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenStreamOpenedAfterDispose_ShouldThrowObjectDisposedException()
    {
        CompoundFile file = OpenSample();
        CompoundStreamEntry entry = file.RootStorage.EnumerateStreams().First();
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => entry.ReadAllBytes());
    }
}
