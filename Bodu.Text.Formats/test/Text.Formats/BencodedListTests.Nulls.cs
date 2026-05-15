// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedListTests.Nulls.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

public sealed partial class BencodedListTests
{
    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> items enumerable with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenItemsIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new BencodedList(null!);
        });

        Assert.AreEqual("items", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor rejects an enumerable that contains a <see langword="null" /> element with
    /// <see cref="ArgumentException" /> using the resx-backed message.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenItemsContainsNullElement_ShouldThrowArgumentException()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new BencodedList(new BencodedValue[] { null! });
        });

        Assert.AreEqual("items", ex.ParamName);
        StringAssert.Contains(ex.Message, FormatsResourceStrings.ArgumentException_NullListElement);
    }
}
