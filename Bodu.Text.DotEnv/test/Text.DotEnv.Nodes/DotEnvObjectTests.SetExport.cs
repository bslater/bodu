// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DotEnvObjectTests.SetExport.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.DotEnv.Nodes;

/// <summary>
/// Verifies the export-flag surface of <see cref="DotEnvObject" />, the feature that distinguishes this DOM from its
/// sibling formats.
/// </summary>
public partial class DotEnvObjectTests
{
    /// <summary>
    /// Verifies that a new object reports the object value kind and is empty.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaultConstructed_ShouldBeEmptyObject()
    {
        var sut = new DotEnvObject();

        Assert.AreEqual(DotEnvValueKind.Object, sut.ValueKind);
        Assert.AreEqual(0, sut.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DotEnvObject.TryGetValue(string, out DotEnvValue)" /> returns the stored value for a
    /// present key and reports failure for an absent one.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenKeyPresent_ShouldReturnTrueAndValue()
    {
        var expected = new DotEnvValue("production");
        var sut = new DotEnvObject { ["ENVIRONMENT"] = expected };

        Assert.IsTrue(sut.TryGetValue("ENVIRONMENT", out DotEnvValue? actual));
        Assert.AreSame(expected, actual);

        Assert.IsFalse(sut.TryGetValue("MISSING", out DotEnvValue? absent));
        Assert.IsNull(absent);
    }

    /// <summary>
    /// Verifies that a key is not exported until it is marked so.
    /// </summary>
    [TestMethod]
    public void SetExport_WhenNotSet_ShouldNotBeExported()
    {
        var sut = new DotEnvObject { ["TOKEN"] = new DotEnvValue("abc") };

        Assert.IsFalse(sut.IsExport("TOKEN"));
    }

    /// <summary>
    /// Verifies that marking a key exported is observable through <see cref="DotEnvObject.IsExport(string)" />.
    /// </summary>
    [TestMethod]
    public void SetExport_WhenSetTrue_ShouldMarkKeyExported()
    {
        var sut = new DotEnvObject { ["TOKEN"] = new DotEnvValue("abc") };

        sut.SetExport("TOKEN", true);

        Assert.IsTrue(sut.IsExport("TOKEN"));
    }

    /// <summary>
    /// Verifies that clearing the export flag removes it, so the flag is a toggle rather than write-once.
    /// </summary>
    [TestMethod]
    public void SetExport_WhenSetFalseAfterTrue_ShouldClearTheFlag()
    {
        var sut = new DotEnvObject { ["TOKEN"] = new DotEnvValue("abc") };
        sut.SetExport("TOKEN", true);

        sut.SetExport("TOKEN", false);

        Assert.IsFalse(sut.IsExport("TOKEN"));
    }

    /// <summary>
    /// Verifies that setting the export flag twice is idempotent rather than faulting on the duplicate add.
    /// </summary>
    [TestMethod]
    public void SetExport_WhenSetTrueTwice_ShouldRemainExported()
    {
        var sut = new DotEnvObject { ["TOKEN"] = new DotEnvValue("abc") };

        sut.SetExport("TOKEN", true);
        sut.SetExport("TOKEN", true);

        Assert.IsTrue(sut.IsExport("TOKEN"));
    }

    /// <summary>
    /// Verifies that marking an absent key exported throws <see cref="KeyNotFoundException" />, so the export set
    /// cannot drift out of step with the entries.
    /// </summary>
    [TestMethod]
    public void SetExport_WhenKeyAbsent_ShouldThrowExactly()
    {
        var sut = new DotEnvObject();

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            sut.SetExport("MISSING", true);
        });
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> key throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void SetExport_WhenKeyIsNull_ShouldThrowExactly()
    {
        var sut = new DotEnvObject();

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            sut.SetExport(null!, true);
        });

        Assert.AreEqual("key", ex.ParamName);
    }
}
