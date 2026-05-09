// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305Tests.Initialize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public partial class Poly1305Tests
{
    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> on a disposed
    /// <see cref="Poly1305" /> instance throws <see cref="ObjectDisposedException" /> rather than
    /// invoking <c>OnKeyChanged</c> against cleared state.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var poly = new Poly1305();
        poly.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            poly.Initialize();
        });
    }
}
