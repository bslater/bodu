// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;
using Bodu.Test;

namespace Bodu.Security.Cryptography;

public abstract partial class BlockCipherTests<TTest, TCipher, TVariant>
{
    /// <summary>
    /// Verifies that writable public properties on a hash algorithm throw an <see cref="ObjectDisposedException" /> when set after the
    /// algorithm instance has been disposed.
    /// </summary>
    /// <param name="property">The property to test for post-disposal access.</param>
    /// <remarks>
    /// This test uses reflection to reassign the property's current hashValue after calling <see cref="HashAlgorithm.Dispose" />. This
    /// ensures concrete <see cref="HashAlgorithm" /> implementations enforce correct disposal behaviour.
    /// </remarks>
    [TestMethod(UnfoldingStrategy = TestDataSourceUnfoldingStrategy.Unfold)]
    [DynamicData(nameof(GetAlgorithmWritableProperties), DynamicDataDisplayName = nameof(TestHelpers.GetTypePropertyDisplayName), DynamicDataDisplayNameDeclaringType = typeof(TestHelpers))]
    public void Dispose_WhenAssigningProperty_ShouldThrowExactly(PropertyInfo property)
    {
        if (property is null)
        {
            Assert.Inconclusive($"Type '{typeof(TCipher).Name}' has no writable properties - test passes by default.");
            return;
        }

        using TCipher cipher = CreateBlockCipher();

        object? currentValue;
        try
        {
            currentValue = property.GetValue(cipher);
        }
        catch
        {
            Assert.Inconclusive($"Property '{property.Name}' could not be read before disposal.");
            return;
        }

        cipher.Dispose();

        try
        {
            property.SetValue(cipher, currentValue);
            Assert.Fail($"Expected ObjectDisposedException when setting property '{property.Name}' after disposal.");
        }
        catch (TargetInvocationException tie) when (tie.InnerException is ObjectDisposedException)
        {
            // Expected: disposed object should not allow configuration
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception when setting property '{property.Name}' after disposal: {ex.GetType().Name} - {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that all fields of a disposed hash algorithm instance have been properly cleared or zeroed.
    /// </summary>
    /// <typeparam name="TCipher">The hash algorithm type under test.</typeparam>
    /// <param name="field">The field to validate after disposal.</param>
    /// <summary>
    /// Verifies that a disposable algorithm properly zeroes or nullifies its internal fields after disposal.
    /// </summary>
    /// <param name="field">The field to inspect for zeroed or null state.</param>
    [TestMethod(UnfoldingStrategy = TestDataSourceUnfoldingStrategy.Unfold)]
    [DynamicData(nameof(GetAlgorithmFields), DynamicDataDisplayName = nameof(TestHelpers.GetTypeFieldDisplayName), DynamicDataDisplayNameDeclaringType = typeof(TestHelpers))]
    public void Dispose_WhenCalled_ShouldZeroDeclaredField(FieldInfo field)
    {
        if (field is null)
        {
            Assert.Inconclusive($"Type '{typeof(TCipher).Name}' has no writable fields - test passes by default.");
            return;
        }

        using TCipher cipher = CreateBlockCipher();
        var buffer = new byte[cipher.BlockSize / 8];
        cipher.Encrypt(buffer, buffer);
        cipher.Dispose();

        var value = field.GetValue(cipher);
        var label = $"Field '{field.DeclaringType},{field.Name}'";

        var result = TestHelpers.AssertFieldValueIsNullOrDefault(field, cipher);

        Assert.IsTrue(result, $"{label} value is not null or default");
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> after disposal throws an <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(BlockCipherVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void Dispose_WhenDecryptCalledAfterDispose_ShouldThrowExactly(TVariant variant)
    {
        BlockCipherSpecification specification = GetSpecification(variant);
        using TCipher cipher = CreateBlockCipher(variant);
        cipher.Dispose();

        var buffer = new byte[specification.BlockSize];
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cipher.Decrypt(buffer, buffer);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.ComputeHash(byte[])" /> after disposal throws an <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(BlockCipherVariants), DynamicDataDisplayName = nameof(VariantDisplayNameHelper.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(VariantDisplayNameHelper))]
    public void Dispose_WhenEncryptCalledAfterDispose_ShouldThrowExactly(TVariant variant)
    {
        BlockCipherSpecification specification = GetSpecification(variant);
        using TCipher cipher = CreateBlockCipher(variant);
        cipher.Dispose();

        var buffer = new byte[specification.BlockSize];
        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            cipher.Encrypt(buffer, buffer);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="IDisposable.Dispose" /> twice on the same
    /// <typeparamref name="TCipher" /> instance is idempotent and does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        TCipher cipher = CreateBlockCipher();
        cipher.Dispose();

        try
        {
            cipher.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Second Dispose on {typeof(TCipher).Name} threw {ex.GetType().Name}: {ex.Message}");
        }
    }
}
