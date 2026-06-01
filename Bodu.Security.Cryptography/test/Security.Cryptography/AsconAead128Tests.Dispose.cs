// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsconAead128Tests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Test;

namespace Bodu.Security.Cryptography;

public partial class AsconAead128Tests
{
    /// <summary>
    /// Verifies that disposing an <see cref="AsconAead128" /> instance clears owned sensitive fields.
    /// </summary>
    /// <param name="field">The field to inspect after disposal.</param>
    /// <remarks>
    /// <para>
    /// This test intentionally includes readonly fields so that key halves such as <c>_k0</c> and <c>_k1</c> are covered.
    /// If those fields remain readonly and are not explicitly cleared by <see cref="AsconAead128.Dispose" />, this test fails.
    /// </para>
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(GetDisposableFields), DynamicDataDisplayName = nameof(TestHelpers.GetTypeFieldDisplayName), DynamicDataDisplayNameDeclaringType = typeof(TestHelpers))]
    public void Dispose_WhenCalled_ShouldZeroPrivateField(FieldInfo field)
    {
        if (field is null)
        {
            Assert.Inconclusive($"{nameof(AsconAead128)} declares no disposable fields.");
            return;
        }

        var sut = new AsconAead128(s_validKey, s_validNonce);

        sut.ProcessAssociatedData([0x01, 0x02, 0x03, 0x04]);

        byte[] plaintext = [0x10, 0x20, 0x30, 0x40];
        var ciphertext = new byte[plaintext.Length + AsconAead128.TagBytes];

        _ = sut.Encrypt(plaintext, ciphertext);

        sut.Dispose();

        var label = $"Field '{field.DeclaringType?.Name}.{field.Name}' was not cleared by Dispose.";

        Assert.IsTrue(
            TestHelpers.AssertFieldValueIsNullOrDefault(field, sut),
            label);
    }

    /// <summary>
    /// Verifies that calling <see cref="AsconAead128.Dispose" /> more than once is safe.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        var sut = new AsconAead128(s_validKey, s_validNonce);

        sut.Dispose();

        sut.Dispose();
    }
}