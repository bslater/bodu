// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Poly1305CoreTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public sealed partial class Poly1305CoreTests
{
    /// <summary>
    /// Verifies that clearing a keyed core part way through a message zeroes every byte of it: key schedule, <c>s</c>,
    /// accumulator and held bytes.
    /// </summary>
    [TestMethod]
    public void Clear_WhenCoreHoldsState_ShouldZeroEveryByte()
    {
        var random = new Random(0x1305_0009);
        Poly1305Core core = default;
        core.Initialize(NextBytes(random, Poly1305Core.KeyBytes));
        core.Update(NextBytes(random, 39));
        Assert.IsFalse(IsCleared(ref core), "Precondition: the core should hold key and message state.");

        core.Clear();

        Assert.IsTrue(IsCleared(ref core), "Clear must zero the core.");
    }
}
