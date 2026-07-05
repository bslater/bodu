// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.None.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that None reports no value present.
    /// </summary>
    [TestMethod]
    public void None_WhenAccessed_ShouldReportIsNone()
    {
        var option = Option<string>.None;

        Assert.IsTrue(option.IsNone);
        Assert.IsFalse(option.IsSome);
    }

    /// <summary>
    /// Verifies the documented contract that an uninitialized option equals None.
    /// </summary>
    [TestMethod]
    public void None_WhenComparedToDefaultInstance_ShouldBeEqual()
    {
        Assert.AreEqual(Option<int>.None, default(Option<int>));
        Assert.AreEqual(Option<string>.None, default(Option<string>));
    }

    /// <summary>
    /// Verifies that the non-generic companion factory returns None of the requested type.
    /// </summary>
    [TestMethod]
    public void None_WhenCreatedViaCompanionClass_ShouldMatchGenericProperty()
    {
        Assert.AreEqual(Option<int>.None, Option.None<int>());
    }
}
