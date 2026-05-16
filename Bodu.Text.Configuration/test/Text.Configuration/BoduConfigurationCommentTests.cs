// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationCommentTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Tests for the <see cref="BoduConfigurationComment" /> readonly struct: construction, equality, and string
/// rendering.
/// </summary>
[TestClass]
public class BoduConfigurationCommentTests
{
    /// <summary>
    /// Verifies that the ctor stores the supplied prefix, text, and location.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenValuesProvided_ShouldExposeAllFields()
    {
        BoduConfigurationSourceLocation location = new(5, 1, 10);
        BoduConfigurationComment comment = new('#', " note", location);

        Assert.AreEqual('#', comment.Prefix);
        Assert.AreEqual(" note", comment.Text);
        Assert.AreEqual(location, comment.Location);
    }

    /// <summary>
    /// Verifies that the ctor rejects a prefix character that is neither <c>#</c> nor <c>;</c>.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenPrefixIsInvalid_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = new BoduConfigurationComment('!', " note", default));
    }

    /// <summary>
    /// Verifies that the ctor rejects a <see langword="null" /> text.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTextIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new BoduConfigurationComment('#', null!, default));
    }

    /// <summary>
    /// Verifies that comments with the same prefix, text, and location compare equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenAllFieldsMatch_ShouldBeTrue()
    {
        BoduConfigurationComment a = new('#', " note", default);
        BoduConfigurationComment b = new('#', " note", default);

        Assert.AreEqual(a, b);
        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
    }

    /// <summary>
    /// Verifies that comments differing in prefix do not compare equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenPrefixDiffers_ShouldBeFalse()
    {
        BoduConfigurationComment a = new('#', " note", default);
        BoduConfigurationComment b = new(';', " note", default);

        Assert.AreNotEqual(a, b);
    }

    /// <summary>
    /// Verifies that <see cref="BoduConfigurationComment.ToString" /> renders prefix followed by text.
    /// </summary>
    [TestMethod]
    public void ToString_WhenRendered_ShouldConcatPrefixAndText()
    {
        BoduConfigurationComment comment = new('#', " hello", default);

        Assert.AreEqual("# hello", comment.ToString());
    }
}
