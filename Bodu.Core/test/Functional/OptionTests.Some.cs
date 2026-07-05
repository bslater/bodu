// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OptionTests.Some.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Functional;

public sealed partial class OptionTests
{
    /// <summary>
    /// Verifies that Some stores the supplied reference-type value and reports it present.
    /// </summary>
    [TestMethod]
    public void Some_WhenValueIsReferenceType_ShouldStoreValue()
    {
        var option = Option<string>.Some("value");

        Assert.IsTrue(option.IsSome);
        Assert.AreEqual("value", option.Value);
    }

    /// <summary>
    /// Verifies that Some stores the supplied value-type value and reports it present.
    /// </summary>
    [TestMethod]
    public void Some_WhenValueIsValueType_ShouldStoreValue()
    {
        var option = Option<int>.Some(0);

        Assert.IsTrue(option.IsSome);
        Assert.AreEqual(0, option.Value);
    }

    /// <summary>
    /// Verifies that the strict Some factory rejects <see langword="null" /> with
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Some_WhenValueIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Option<string>.Some(null!);
        });

        Assert.AreEqual("value", ex.ParamName);
    }

    /// <summary>
    /// Verifies that the non-generic companion factory infers the option type from its argument.
    /// </summary>
    [TestMethod]
    public void Some_WhenCreatedViaCompanionClass_ShouldMatchGenericFactory()
    {
        var viaCompanion = Option.Some(42);
        var viaGeneric = Option<int>.Some(42);

        Assert.AreEqual(viaGeneric, viaCompanion);
    }
}
