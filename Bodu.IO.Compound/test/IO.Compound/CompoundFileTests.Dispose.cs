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
    /// Verifies that opening a stream after the file has been disposed throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenStreamOpenedAfterDispose_ShouldThrowObjectDisposedException()
    {
        CompoundFile file = OpenSample();
        string name = file.RootStorage.EnumerateStreams().First().Name;
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => file.RootStorage.OpenStream(name));
    }
}
