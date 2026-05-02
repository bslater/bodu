// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadBlockCipherModeTests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using System.Reflection;

namespace Bodu.Security.Cryptography;

public abstract partial class AeadBlockCipherModeTests<TTransform>
{
    /// <summary>
    /// Names of fields whose values are allowed to remain non-default after disposal because they
    /// represent lifecycle bookkeeping rather than sensitive cryptographic material.
    /// </summary>
    private static readonly string[] DisposableFieldExclusions =
    [
        "_disposed",
        "_completed",
        "_aadProcessed",
    ];

    /// <summary>
    /// Enumerates the writable instance fields of <typeparamref name="TTransform" /> that should be
    /// cleared, zeroed, or nullified by <see cref="System.IDisposable.Dispose" />.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays, each wrapping one <see cref="FieldInfo" />
    /// suitable for use with <see cref="DynamicDataAttribute" />.
    /// </returns>
    public static IEnumerable<object[]> GetDisposableFields() =>
        TestHelpers.GetFieldInfoForType<TTransform>(
            excludeReadOnly: true,
            excludeFileds: DisposableFieldExclusions);

    /// <summary>
    /// Enumerates the publicly writable instance properties of <typeparamref name="TTransform" />.
    /// AEAD transforms typically expose no writable properties, in which case this enumeration is
    /// empty and the corresponding tests resolve to <see cref="Assert.Inconclusive(string)" />.
    /// </summary>
    /// <returns>
    /// A sequence of single-element object arrays, each wrapping one <see cref="PropertyInfo" />.
    /// </returns>
    public static IEnumerable<object[]> GetWritableProperties() =>
        TestHelpers.GetPropertyInfoForType<TTransform>(TestHelpers.PropertyAccessMode.Write);

    // ── Dispose lifecycle ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that calling <see cref="System.IDisposable.Dispose" /> more than once on the same
    /// transform instance does not throw.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var transform = MakeTransform();

        transform.Dispose();
        transform.Dispose();
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Encrypt" /> throws
    /// <see cref="ObjectDisposedException" /> when called on a disposed transform.
    /// </summary>
    [TestMethod]
    public void Encrypt_WhenCalledAfterDispose_ShouldThrowObjectDisposedException()
    {
        var transform = MakeTransform();
        transform.Dispose();

        var plaintext = new byte[ExpectedBlockSize];
        var output = new byte[plaintext.Length + 16];

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
            transform.Encrypt(plaintext, output));
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.Decrypt" /> throws
    /// <see cref="ObjectDisposedException" /> when called on a disposed transform.
    /// </summary>
    [TestMethod]
    public void Decrypt_WhenCalledAfterDispose_ShouldThrowObjectDisposedException()
    {
        var transform = MakeTransform();
        transform.Dispose();

        var ciphertextWithTag = new byte[ExpectedBlockSize + 16];
        var output = new byte[ExpectedBlockSize];

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
            transform.Decrypt(ciphertextWithTag, output));
    }

    /// <summary>
    /// Verifies that <see cref="IAeadBlockCipherModeTransform.ProcessAssociatedData" /> throws
    /// <see cref="ObjectDisposedException" /> when called on a disposed transform.
    /// </summary>
    [TestMethod]
    public void ProcessAssociatedData_WhenCalledAfterDispose_ShouldThrowObjectDisposedException()
    {
        var transform = MakeTransform();
        transform.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
            transform.ProcessAssociatedData(new byte[] { 0x01, 0x02, 0x03 }));
    }

    // ── Sensitive field clearing ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that every writable instance field of the disposed transform — excluding lifecycle
    /// bookkeeping flags — has been cleared, zeroed, or nullified by
    /// <see cref="System.IDisposable.Dispose" />, ensuring no sensitive cryptographic state
    /// (subkeys, counters, accumulators, cached AAD) lingers in memory after disposal.
    /// </summary>
    /// <param name="field">The field to inspect after disposal.</param>
    [TestMethod]
    [DynamicData(
        nameof(GetDisposableFields),
        DynamicDataDisplayName = nameof(TestHelpers.GetDisposableFieldDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TestHelpers))]
    public void Dispose_WhenCalled_ShouldZeroPrivateField(FieldInfo field)
    {
        if (field is null)
        {
            Assert.Inconclusive($"Type '{typeof(TTransform).Name}' has no writable fields - test passes by default.");
            return;
        }

        var transform = MakeTransform();

        transform.ProcessAssociatedData(new byte[] { 0x01, 0x02, 0x03, 0x04 });

        var plaintext = new byte[ExpectedBlockSize];
        var output = new byte[plaintext.Length + transform.TagSize];
        _ = transform.Encrypt(plaintext, output);

        transform.Dispose();

        string label = $"Field '{field.DeclaringType?.Name}.{field.Name}' was not cleared by Dispose.";

        Assert.IsTrue(
            TestHelpers.AssertFieldValueIsNullOrDefault(field, transform),
            label);
    }

    // ── Writable-property post-disposal contract ─────────────────────────────────────────────

    /// <summary>
    /// Verifies that assigning to any publicly writable property on a disposed transform throws
    /// <see cref="ObjectDisposedException" />, and that the exception's
    /// <see cref="ObjectDisposedException.ObjectName" /> matches the concrete transform type.
    /// AEAD transforms typically expose no writable properties; in that case the test resolves
    /// to <see cref="Assert.Inconclusive(string)" />.
    /// </summary>
    /// <param name="property">The writable property to assign after disposal.</param>
    [TestMethod]
    [DynamicData(
        nameof(GetWritableProperties),
        DynamicDataDisplayName = nameof(TestHelpers.GetDisposablePropertyDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TestHelpers))]
    public void Dispose_WhenAssigningProperty_ShouldThrowExactly(PropertyInfo property)
    {
        if (property is null)
        {
            Assert.Inconclusive($"Type '{typeof(TTransform).Name}' has no writable properties - test passes by default.");
            return;
        }

        var transform = MakeTransform();

        object? currentValue;
        try
        {
            currentValue = property.GetValue(transform);
        }
        catch
        {
            Assert.Inconclusive($"Property '{property.Name}' could not be read before disposal.");
            return;
        }

        transform.Dispose();

        try
        {
            property.SetValue(transform, currentValue);
            Assert.Fail($"Expected ObjectDisposedException when setting property '{property.Name}' after disposal.");
        }
        catch (TargetInvocationException tie) when (tie.InnerException is ObjectDisposedException ex)
        {
            Assert.AreEqual(
                typeof(TTransform).FullName,
                ex.ObjectName,
                $"ObjectDisposedException.ObjectName must match the concrete type name '{typeof(TTransform).FullName}'.");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception when setting property '{property.Name}' after disposal: {ex.GetType().Name} - {ex.Message}");
        }
    }
}
