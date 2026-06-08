// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyRegistryTests.Replace.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CurrencyRegistryTests
{
    /// <summary>
    /// Verifies that <see cref="CurrencyRegistry.Replace(CurrencyInfo)" /> overwrites an existing custom registration.
    /// </summary>
    [TestMethod]
    public void Replace_WhenExistingCustom_ShouldOverwrite()
    {
        CurrencyRegistry.Register(new CurrencyInfo("XQF", 2, 0m, false, null, null));

        CurrencyRegistry.Replace(new CurrencyInfo("XQF", 3, 0m, false, null, null));

        Assert.IsTrue(CurrencyRegistry.TryGet("XQF", out CurrencyInfo? resolved));
        Assert.AreEqual(3, resolved!.MinorUnits);
    }

    /// <summary>
    /// Verifies that the conflict-policy overload keeps the existing registration under
    /// <see cref="CurrencyRegistrationConflictPolicy.Ignore" />.
    /// </summary>
    [TestMethod]
    public void Register_WhenConflictPolicyIgnore_ShouldKeepExisting()
    {
        CurrencyRegistry.Register(new CurrencyInfo("XQG", 2, 0m, false, null, null));

        CurrencyRegistry.Register(new CurrencyInfo("XQG", 3, 0m, false, null, null), CurrencyRegistrationConflictPolicy.Ignore);

        Assert.IsTrue(CurrencyRegistry.TryGet("XQG", out CurrencyInfo? resolved));
        Assert.AreEqual(2, resolved!.MinorUnits);
    }

    /// <summary>
    /// Verifies that the conflict-policy overload overwrites the existing registration under
    /// <see cref="CurrencyRegistrationConflictPolicy.Replace" />.
    /// </summary>
    [TestMethod]
    public void Register_WhenConflictPolicyReplace_ShouldOverwrite()
    {
        CurrencyRegistry.Register(new CurrencyInfo("XQI", 2, 0m, false, null, null));

        CurrencyRegistry.Register(new CurrencyInfo("XQI", 3, 0m, false, null, null), CurrencyRegistrationConflictPolicy.Replace);

        Assert.IsTrue(CurrencyRegistry.TryGet("XQI", out CurrencyInfo? resolved));
        Assert.AreEqual(3, resolved!.MinorUnits);
    }

    /// <summary>
    /// Verifies that the conflict-policy overload throws under <see cref="CurrencyRegistrationConflictPolicy.Throw" />
    /// when a custom registration already exists.
    /// </summary>
    [TestMethod]
    public void Register_WhenConflictPolicyThrow_ShouldThrowInvalidOperationException()
    {
        CurrencyRegistry.Register(new CurrencyInfo("XQJ", 2, 0m, false, null, null));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            CurrencyRegistry.Register(new CurrencyInfo("XQJ", 3, 0m, false, null, null), CurrencyRegistrationConflictPolicy.Throw);
        });
    }

    /// <summary>
    /// Verifies that the conflict-policy overload rejects an undefined policy value with
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Register_WhenConflictPolicyUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CurrencyRegistry.Register(new CurrencyInfo("XQK", 2, 0m, false, null, null), (CurrencyRegistrationConflictPolicy)99);
        });

        Assert.AreEqual("conflictPolicy", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="CurrencyRegistry.Replace(CurrencyInfo)" /> validates metadata before storing it.
    /// </summary>
    [TestMethod]
    public void Replace_WhenMetadataInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            CurrencyRegistry.Replace(new CurrencyInfo("XQL", 29, 0m, false, null, null));
        });
    }
}
