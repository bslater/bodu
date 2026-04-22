// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherTests.Dispose.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using Bodu.Test;
using System.Reflection;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class BlockCipherTests<TTest, TCipher, TVariant>
    {
        private static string[] IgnoreFieldNames => new[]
        {
            "HashSizeValue",
            "State",
            "disposed",
            "_disposed",
        };

        /// <summary>
        /// Enumerates all instance fields in the algorithm and its base types to validate disposal state.
        /// </summary>
        public static IEnumerable<object[]> GetDisposableFields() => TestHelpers.GetFieldInfoForType<TCipher>(excludeFileds: "disposed");

        /// <summary>
        /// Verifies that writable public properties on a hash algorithm throw an <see cref="ObjectDisposedException" /> when set after the
        /// algorithm instance has been disposed.
        /// </summary>
        /// <param name="property">The property to test for post-disposal access.</param>
        /// <remarks>
        /// This test uses reflection to reassign the property's current hashValue after calling <see cref="HashAlgorithm.Dispose" />. This
        /// ensures concrete <see cref="HashAlgorithm" /> implementations enforce correct disposal behavior.
        /// </remarks>
        [TestMethod]
        [DynamicData(nameof(GetWritableProperties), DynamicDataSourceType.Method)]
        public void Dispose_WhenAssigningProperty_ShouldThrowExactly(PropertyInfo property)
        {
            if (property is null)
            {
                Assert.Inconclusive($"Type '{typeof(TCipher).Name}' has no writable properties - test passes by default.");
                return;
            }

            using var cipher = CreateBlockCipher();

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
                // ✅ Expected: disposed object should not allow configuration
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
        [TestMethod]
        [DynamicData(nameof(GetDisposableFields), DynamicDataSourceType.Method)]
        public void Dispose_WhenCalled_ShouldZeroPrivateField(FieldInfo field)
        {
            if (field is null)
            {
                Assert.Inconclusive($"Type '{typeof(TCipher).Name}' has no writable fields - test passes by default.");
                return;
            }

            using var cipher = CreateBlockCipher();
            var buffer = new byte[cipher.BlockSize];
            cipher.Encrypt(buffer, buffer);
            cipher.Dispose();

            object? value = field.GetValue(cipher);
            string label = $"Field '{field.DeclaringType},{field.Name}'";

            var result = TestHelpers.AssertFieldValueIsNullOrDefault(field, cipher);

            Assert.IsTrue(result, $"{label} value is not null or default");
        }

        /// <summary>
        /// Verifies that calling <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> after disposal throws an <see cref="ObjectDisposedException" />.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(BlockCipherVariants))]
        public void Dispose_WhenDecryptCalledAfterDispose_ShouldThrowExactly(TVariant variant)
        {
            var specification = GetSpecification(variant);
            using var cipher = CreateBlockCipher(variant);
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
        [DynamicData(nameof(BlockCipherVariants))]
        public void Dispose_WhenEncryptCalledAfterDispose_ShouldThrowExactly(TVariant variant)
        {
            var specification = GetSpecification(variant);
            using var cipher = CreateBlockCipher(variant);
            cipher.Dispose();

            var buffer = new byte[specification.BlockSize];
            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                cipher.Encrypt(buffer, buffer);
            });
        }
    }
}