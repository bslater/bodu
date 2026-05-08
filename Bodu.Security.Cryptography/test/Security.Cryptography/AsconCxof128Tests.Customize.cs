// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconCxof128Tests.Customize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconCxof128Tests
{
    /// <summary>
    /// Verifies that calling <see cref="AsconCxof128.Customize" /> a second time throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Customize_WhenCalledTwice_ShouldThrowInvalidOperationException()
    {
        using AsconCxof128 sut = new AsconCxof128();
        sut.Customize([0x01]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.Customize([0x02]);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="AsconCxof128.Customize" /> after <see cref="AsconXof{T}.Absorb" />
    /// has been called throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Customize_AfterAbsorb_ShouldThrowInvalidOperationException()
    {
        using AsconCxof128 sut = new AsconCxof128();
        sut.Absorb([0x01]);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            sut.Customize([0x02]);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="AsconCxof128.Customize" /> on a disposed instance throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Customize_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        AsconCxof128 sut = new AsconCxof128();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Customize([0x01]);
        });
    }
}
