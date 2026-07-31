// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstFileTests
{
    /// <summary>
    /// Verifies that disposing twice is harmless.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        PstFile file = OpenSample1();

        file.Dispose();
        file.Dispose();
    }

    /// <summary>
    /// Verifies that the header-fact properties refuse access after disposal.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenDisposed_ShouldGateHeaderProperties()
    {
        PstFile file = OpenSample1();
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = file.Format;
        });

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = file.CryptMethod;
        });
    }

    /// <summary>
    /// Verifies that a node obtained before disposal refuses data access afterwards.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenDisposed_ShouldGateEarlierNodes()
    {
        PstFile file = OpenSample1();
        PstNode node = file.GetNode(PstNodeId.MessageStore);
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = node.ReadAllBytes();
        });
    }
}
