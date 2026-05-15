// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedIntegerTests.Equality.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

public sealed partial class BencodedIntegerTests
{

    /// <summary>
    /// Verifies that <see cref="object.GetHashCode" /> is consistent with the equality contract: equal values
    /// yield equal hash codes.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenSameValue_ShouldReturnSameHash()
    {
        BencodedInteger a = new(42);
        BencodedInteger b = new(42);

        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that <see cref="object.Equals(object)" /> returns <see langword="false" /> when compared with an
    /// instance of an unrelated type.
    /// </summary>
    [TestMethod]
    public void ObjectEquals_WhenComparedWithUnrelatedType_ShouldReturnFalse()
    {
        BencodedInteger a = new(42);

        Assert.IsFalse(a.Equals("not an integer"));
    }

    /// <summary>
    /// Verifies that <see cref="object.Equals(object)" /> follows the same contract as the typed equality.
    /// </summary>
    [TestMethod]
    public void ObjectEquals_WhenSameValue_ShouldReturnTrue()
    {
        BencodedInteger a = new(42);
        object b = new BencodedInteger(42);

        Assert.IsTrue(a.Equals(b));
    }

    /// <summary>
    /// Verifies that two <see cref="BencodedInteger" /> values with different integers compare unequal via the
    /// typed <see cref="IEquatable{T}.Equals" /> overload.
    /// </summary>
    [TestMethod]
    public void TypedEquals_WhenDifferentValues_ShouldReturnFalse()
    {
        BencodedInteger a = new(42);
        BencodedInteger b = new(43);

        Assert.IsFalse(a.Equals(b));
    }

    /// <summary>
    /// Verifies that comparing a <see cref="BencodedInteger" /> to a <see langword="null" /> reference via the
    /// typed equals overload returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TypedEquals_WhenOtherIsNull_ShouldReturnFalse()
    {
        BencodedInteger a = new(42);

        Assert.IsFalse(a.Equals((BencodedInteger?)null));
    }
    /// <summary>
    /// Verifies that two <see cref="BencodedInteger" /> values with the same integer compare equal via the typed
    /// <see cref="IEquatable{T}.Equals" /> overload.
    /// </summary>
    [TestMethod]
    public void TypedEquals_WhenSameValue_ShouldReturnTrue()
    {
        BencodedInteger a = new(42);
        BencodedInteger b = new(42);

        Assert.IsTrue(a.Equals(b));
    }

}
