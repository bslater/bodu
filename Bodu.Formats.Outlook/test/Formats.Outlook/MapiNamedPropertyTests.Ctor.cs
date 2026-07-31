// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiNamedPropertyTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Formats.Outlook;

public partial class MapiNamedPropertyTests
{
    /// <summary>
    /// Verifies that the numeric constructor sets the identifier and leaves the name unset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNumericIdentity_ShouldSetIdAndClearName()
    {
        var named = new MapiNamedProperty(PublicStrings, 0x00008233u);

        Assert.AreEqual(PublicStrings, named.PropertySetId);
        Assert.AreEqual(0x00008233u, named.Id);
        Assert.IsNull(named.Name);
    }

    /// <summary>
    /// Verifies that the string constructor sets the name and leaves the identifier unset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenStringIdentity_ShouldSetNameAndClearId()
    {
        var named = new MapiNamedProperty(PublicStrings, "Keywords");

        Assert.AreEqual(PublicStrings, named.PropertySetId);
        Assert.AreEqual("Keywords", named.Name);
        Assert.IsNull(named.Id);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> name throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenNameIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new MapiNamedProperty(PublicStrings, null!);
            },
            "name");
    }

    /// <summary>
    /// Verifies that an empty or whitespace name throws <see cref="ArgumentException" />.
    /// </summary>
    /// <param name="testName">The scenario label.</param>
    /// <param name="name">The invalid name.</param>
    [TestMethod]
    [DataRow("Empty", "")]
    [DataRow("Whitespace", "   ")]
    public void Ctor_WhenNameIsEmptyOrWhitespace_ShouldThrowArgumentException(string testName, string name)
    {
        _ = testName;

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new MapiNamedProperty(PublicStrings, name);
            },
            "name");
    }
}
