// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedListTests.Nulls.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

public sealed partial class BencodedListTests
{

    /// <summary>
    /// Verifies that the constructor rejects an enumerable that contains a <see langword="null" /> element with
    /// <see cref="ArgumentException" /> using the resx-backed message.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenItemsContainsNullElement_ShouldThrowExactly()
    {
        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new BencodedList([null!]);
        });

        Assert.AreEqual("items", ex.ParamName);
        StringAssert.Contains(ex.Message, FormatsResourceStrings.Arg_Invalid_NullListElement);
    }
    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> items enumerable with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenItemsIsNull_ShouldThrowExactly()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new BencodedList(null!);
        });

        Assert.AreEqual("items", ex.ParamName);
    }

}
