// ---------------------------------------------------------------------------------------------------------------
// <copyright file="KeyedBlockHashAlgorithmTests.Initialize.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

public abstract partial class KeyedBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> on a keyed-hash instance
    /// whose key has been cleared does not silently regenerate a fresh random key. A keyed MAC
    /// that swaps keys on Initialize would violate its security contract. Implementations that
    /// permit silent regeneration may opt out by returning <see langword="true" /> from
    /// <see cref="AllowsKeyRegenerationOnInitialize" />.
    /// </summary>
    [TestMethod]
    public void Initialize_WhenKeyValueIsMissing_ShouldThrowCryptographicException()
    {
        if (AllowsKeyRegenerationOnInitialize)
        {
            Assert.Inconclusive($"{typeof(TAlgorithm).Name} permits silent key regeneration on Initialize.");
            return;
        }

        var algo = CreateAlgorithm();

        // Clear the constructor-seeded random key via the inherited protected KeyValue field.
        System.Reflection.FieldInfo? keyValueField = null;
        for (Type? t = algo.GetType(); t is not null && keyValueField is null; t = t.BaseType)
        {
            keyValueField = t.GetField(
                "KeyValue",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
        }

        Assert.IsNotNull(keyValueField, $"Unable to locate KeyValue field on {typeof(TAlgorithm).Name}.");
        keyValueField!.SetValue(algo, null);

        var ex = Assert.ThrowsExactly<CryptographicException>(() => algo.Initialize());
        Assert.IsTrue(
            ex.Message.IndexOf("key", StringComparison.OrdinalIgnoreCase) >= 0,
            $"Expected exception message to mention 'key'. Actual: '{ex.Message}'");
    }

    /// <summary>
    /// Gets a value indicating whether this keyed-hash implementation permits
    /// <see cref="HashAlgorithm.Initialize" /> to silently regenerate a random key when the
    /// previous key has been cleared. Defaults to <see langword="false" /> (strict keyed-MAC
    /// contract).
    /// </summary>
    protected virtual bool AllowsKeyRegenerationOnInitialize => false;
}
