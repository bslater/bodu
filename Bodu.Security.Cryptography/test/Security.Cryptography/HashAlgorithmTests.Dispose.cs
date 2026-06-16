// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Security.Cryptography;
using Bodu.Test;

namespace Bodu.Security.Cryptography;

public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that readable public properties on a hash algorithm throw an <see cref="ObjectDisposedException" /> when
    /// accessed after the algorithm instance has been disposed.
    /// </summary>
    /// <param name="property">The property to test for post-disposal read access.</param>
    /// <remarks>
    /// This test uses reflection to invoke each property getter after calling <see cref="HashAlgorithm.Dispose" />. This
    /// ensures concrete <see cref="HashAlgorithm" /> implementations enforce correct disposal behaviour on all readable
    /// properties, and that the thrown <see cref="ObjectDisposedException" /> correctly identifies the disposed type.
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(GetAlgorithmReadableProperties), DynamicDataDisplayName = nameof(TestHelpers.GetTypePropertyDisplayName), DynamicDataDisplayNameDeclaringType = typeof(TestHelpers))]
    public void Dispose_WhenReadingProperty_ShouldThrowExactly(PropertyInfo property)
    {
        if (property is null)
        {
            Assert.Inconclusive($"Type '{typeof(TAlgorithm).Name}' has no readable properties - test passes by default.");
            return;
        }

        using TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        try
        {
            _ = property.GetValue(algorithm);
            Assert.Fail($"Expected ObjectDisposedException when reading property '{property.Name}' after disposal.");
        }
        catch (TargetInvocationException tie) when (tie.InnerException is ObjectDisposedException ex)
        {
            // ✅ Expected: disposed object should not allow property reads
            Assert.AreEqual(
                typeof(TAlgorithm).FullName,
                ex.ObjectName,
                $"ObjectDisposedException.ObjectName must match the concrete type name '{typeof(TAlgorithm).FullName}'.");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception when reading property '{property.Name}' after disposal: {ex.GetType().Name} - {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that disposing a freshly-constructed instance of a concrete algoirthm — one
    /// that has never had any property accessed or hashing performed — completes without throwing.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        using TAlgorithm algorithm = CreateAlgorithm();

        try
        {
            algorithm.Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail($"Disposing an untouched {typeof(TAlgorithm).Name} instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that writable public properties on a hash algorithm throw an <see cref="ObjectDisposedException" /> when
    /// assigned after the algorithm instance has been disposed.
    /// </summary>
    /// <param name="property">The property to test for post-disposal write access.</param>
    /// <remarks>
    /// This test uses reflection to read each property's current value before disposal, then attempts to reassign that
    /// same value after calling <see cref="HashAlgorithm.Dispose" />. This ensures concrete <see cref="HashAlgorithm" />
    /// implementations enforce correct disposal behaviour on all writable properties, and that the thrown
    /// <see cref="ObjectDisposedException" /> correctly identifies the disposed type.
    /// </remarks>
    [TestMethod]
    [DynamicData(nameof(GetAlgorithmWritableProperties), DynamicDataDisplayName = nameof(TestHelpers.GetTypeFieldDisplayName), DynamicDataDisplayNameDeclaringType = typeof(TestHelpers))]
    public void Dispose_WhenAssigningProperty_ShouldThrowExactly(PropertyInfo property)
    {
        if (property is null)
        {
            Assert.Inconclusive($"Type '{typeof(TAlgorithm).Name}' has no writable properties - test passes by default.");
            return;
        }

        using TAlgorithm algorithm = CreateAlgorithm();

        object? currentValue;
        try
        {
            currentValue = property.GetValue(algorithm);
        }
        catch
        {
            Assert.Inconclusive($"Property '{property.Name}' could not be read before disposal.");
            return;
        }

        algorithm.Dispose();

        try
        {
            property.SetValue(algorithm, currentValue);
            Assert.Fail($"Expected ObjectDisposedException when setting property '{property.Name}' after disposal.");
        }
        catch (TargetInvocationException tie) when (tie.InnerException is ObjectDisposedException ex)
        {
            // ✅ Expected: disposed object should not allow configuration
            Assert.AreEqual(
                typeof(TAlgorithm).FullName,
                ex.ObjectName,
                $"ObjectDisposedException.ObjectName must match the concrete type name '{typeof(TAlgorithm).FullName}'.");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception when setting property '{property.Name}' after disposal: {ex.GetType().Name} - {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that all fields of a disposed hash algorithm instance have been properly cleared or zeroed.
    /// </summary>
    /// <param name="field">The field to validate after disposal.</param>
    /// <summary>
    /// Verifies that a disposable algorithm properly zeroes or nullifies its internal fields after disposal.
    /// </summary>
    /// <param name="field">The field to inspect for zeroed or null state.</param>
    [TestMethod]
    [DynamicData(nameof(GetAlgorithmFields), DynamicDataDisplayName = nameof(TestHelpers.GetTypeFieldDisplayName), DynamicDataDisplayNameDeclaringType = typeof(TestHelpers))]
    public void Dispose_WhenCalled_ShouldZeroPrivateField(FieldInfo field)
    {
        if (field is null)
        {
            Assert.Inconclusive($"Type '{typeof(TAlgorithm).Name}' has no writable fields - test passes by default.");
            return;
        }

        using TAlgorithm instance = CreateAlgorithm();
        instance.ComputeHash([]);
        instance.Dispose();

        object? value = field.GetValue(instance);
        string label = $"Field '{field.DeclaringType},{field.Name}'";

        bool result = TestHelpers.AssertFieldValueIsNullOrDefault(field, instance);

        Assert.IsTrue(result, $"{label} value '{field.GetValue(instance)}' is not null or default");
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.ComputeHash(byte[], int, int)" /> after disposal throws an <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenComputeHashWithBufferRangeCalledAfterDispose_ShouldThrowExactly()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.ComputeHash([], 0, 0);
        });
    }

    /// <summary>
    /// Verifies that calling <see cref="HashAlgorithm.ComputeHash(Stream)" /> after disposal throws an <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenComputeHashWithStreamCalledAfterDispose_ShouldThrowExactly()
    {
        TAlgorithm algorithm = CreateAlgorithm();
        algorithm.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
            _ = algorithm.ComputeHash(Stream.Null));
    }

    private static bool AssertMemorySpan(Type fieldType, object? value, string label)
    {
        if (fieldType.IsGenericType)
        {
            Type def = fieldType.GetGenericTypeDefinition();
            Type elementType = fieldType.GetGenericArguments()[0];

            if (elementType == typeof(byte))
            {
                if (def == typeof(Memory<>))
                {
                    var memory = (Memory<byte>)value!;
                    foreach (byte b in memory.Span)
                    {
                        Assert.AreEqual(0, b, $"{label} contains non-zero byte.");
                    }
                    return true;
                }
                if (def == typeof(ReadOnlyMemory<>))
                {
                    var memory = (ReadOnlyMemory<byte>)value!;
                    foreach (byte b in memory.Span)
                    {
                        Assert.AreEqual(0, b, $"{label} contains non-zero byte.");
                    }
                    return true;
                }
            }
        }
        return false;
    }

    private static bool AssertPrimitiveArray(Type fieldType, object? value, string label)
    {
        switch (value)
        {
            case byte[] byteArray:
                Assert.IsTrue(byteArray.All(b => b == 0), $"{label} contains non-zero bytes.");
                return true;

            case uint[] uintArray:
                Assert.IsTrue(uintArray.All(b => b == 0), $"{label} contains non-zero elements.");
                return true;

            case Array array when fieldType.IsArray && fieldType.GetElementType()?.IsPrimitive == true:
                Assert.IsNotNull(array, $"{label} is null.");
                object defaultValue = Activator.CreateInstance(fieldType.GetElementType()!)!;
                foreach (object? item in array)
                    Assert.AreEqual(defaultValue, item, $"{label} contains non-zero element '{item}'.");
                return true;
        }
        return false;
    }

    private static bool AssertPrimitiveValue(Type fieldType, object? value, string label)
    {
        switch (value)
        {
            case null:
                return true;

            case int i:
                Assert.AreEqual(0, i, $"{label} is not zero.");
                return true;

            case bool b:
                Assert.IsFalse(b, $"{label} is not false.");
                return true;

            case long l:
                Assert.AreEqual(0L, l, $"{label} is not zero.");
                return true;

            case uint ui:
                Assert.AreEqual(0U, ui, $"{label} is not zero.");
                return true;

            case ulong ul:
                Assert.AreEqual(0UL, ul, $"{label} is not zero.");
                return true;
        }
        return false;
    }

}
