// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TextFilterBuilderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Filtering;

/// <summary>
/// Tests for <see cref="TextFilterBuilder" />.
/// </summary>
[TestClass]
public partial class TextFilterBuilderTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> options argument throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOptionsIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new TextFilterBuilder(null!);
        });

        Assert.AreEqual("options", ex.ParamName);
    }
}
