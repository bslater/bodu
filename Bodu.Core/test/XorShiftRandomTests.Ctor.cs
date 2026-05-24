// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XorShiftRandomTests.Ctor.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class XorShiftRandomTests
{

    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Constructor" />, when CalledWithoutSeed, returns a non-null value.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCalledWithoutSeed_ShouldCreateInstance()
    {
        var rng = new XorShiftRandom();
        Assert.IsNotNull(rng);
    }
    /// <summary>
    /// Verifies that <see cref="XorShiftRandom.Constructor" />, when ValidRange, returns a non-null value.
    /// </summary>
    [TestMethod]
    [DataRow(int.MinValue)]
    [DataRow(0)]
    [DataRow(int.MaxValue)]
    public void Ctor_WhenValidRange_ShouldCreateInstance(int seed)
    {
        var rng = new XorShiftRandom(seed);
        Assert.IsNotNull(rng);
    }

}
