// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconCxof128Tests.Initialize.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class AsconCxof128Tests
{
    /// <summary>
    /// Verifies that <see cref="AsconXof{T}.Initialize" /> resets the customisation state so that
    /// <see cref="AsconCxof128.Customize" /> can be called again on the same instance.
    /// </summary>
    [TestMethod]
    public void Initialize_AfterCustomize_ShouldAllowFreshCustomization()
    {
        using var sut = new AsconCxof128();
        sut.Customize([0x01]);
        sut.Absorb([0x02]);

        sut.Initialize();

        // After reset, customisation must succeed without throwing.
        sut.Customize([0x03]);
        sut.Absorb([0x04]);
        var output = sut.GetHash(32);
        Assert.AreEqual(32, output.Length);
    }

    /// <summary>
    /// Verifies that <see cref="AsconXof{T}.Initialize" /> on a disposed
    /// <see cref="AsconCxof128" /> instance throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenCalledAfterDispose_ShouldThrowExactly()
    {
        var sut = new AsconCxof128();
        sut.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            sut.Initialize();
        });
    }
}
