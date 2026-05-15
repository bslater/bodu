// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvTests.TryParse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats.DotEnv;

public sealed partial class DotEnvTests
{

    /// <summary>
    /// Verifies that <see cref="DotEnv.TryParse(ReadOnlySpan{char}, out DotEnvDocument)" /> returns
    /// <see langword="true" /> and a non-null document for valid input.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsValid_ShouldReturnTrueAndDocument()
    {
        bool result = DotEnv.TryParse("KEY=value", out DotEnvDocument? document);

        Assert.IsTrue(result);
        Assert.IsNotNull(document);
        Assert.AreEqual("value", document["KEY"]);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.TryParse(ReadOnlySpan{char}, out DotEnvDocument)" /> returns
    /// <see langword="true" /> and an empty document for empty input.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsEmpty_ShouldReturnTrueWithEmptyDocument()
    {
        bool result = DotEnv.TryParse(string.Empty, out DotEnvDocument? document);

        Assert.IsTrue(result);
        Assert.IsNotNull(document);
        Assert.AreEqual(0, document.Entries.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.TryParse(ReadOnlySpan{char}, out DotEnvDocument)" /> returns
    /// <see langword="false" /> and a <see langword="null" /> document for an invalid key.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenKeyIsInvalid_ShouldReturnFalseWithNull()
    {
        bool result = DotEnv.TryParse("1INVALID=value", out DotEnvDocument? document);

        Assert.IsFalse(result);
        Assert.IsNull(document);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.TryParse(ReadOnlySpan{char}, DotEnvParseOptions, out DotEnvDocument)" />
    /// respects the supplied options.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenDuplicateKeyDisallowed_ShouldReturnFalseOnDuplicate()
    {
        DotEnvParseOptions options = new() { DuplicateKeyBehavior = DotEnvDuplicateKeyBehavior.Disallowed };

        bool result = DotEnv.TryParse("KEY=a\nKEY=b", options, out DotEnvDocument? document);

        Assert.IsFalse(result);
        Assert.IsNull(document);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnv.TryParse(ReadOnlySpan{char}, out DotEnvDocument)" /> does not throw even
    /// when the input is malformed.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputIsMalformed_ShouldNotThrow()
    {
        bool result = false;
        DotEnvDocument? document = null;

        Exception? caughtException = null;

        try
        {
            result = DotEnv.TryParse("\"unclosed", out document);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        Assert.IsNull(caughtException, $"TryParse threw {caughtException?.GetType().Name}.");
        Assert.IsFalse(result);
        Assert.IsNull(document);
    }

}
