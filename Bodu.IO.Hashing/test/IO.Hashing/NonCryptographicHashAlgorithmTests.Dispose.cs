// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.Dispose.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;
using System.Reflection;
using Bodu.Test;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{

    /// <summary>
    /// Verifies that calling <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" /> after
    /// disposal throws <see cref="ObjectDisposedException" />.
    /// </summary>
    /// <remarks>
    /// Skipped (inconclusive) when <typeparamref name="TAlgorithm" /> does not implement <see cref="IDisposable" />.
    /// </remarks>
    [TestMethod]
    public void Dispose_WhenAppendCalledAfterDispose_ShouldThrowExactly()
    {
        if (!typeof(TAlgorithm).IsAssignableTo(typeof(IDisposable)))
        {
            Assert.Inconclusive(
                $"Type '{typeof(TAlgorithm).Name}' does not implement IDisposable; disposal Append test skipped.");
            return;
        }

        TAlgorithm algorithm = CreateAlgorithm();
        ((IDisposable)algorithm).Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            algorithm.Append(ReadOnlySpan<byte>.Empty);
        });
    }

    /// <summary>
    /// Verifies that writable public properties on the algorithm throw <see cref="ObjectDisposedException" /> when
    /// assigned after the instance has been disposed.
    /// </summary>
    /// <param name="property">The property to test for post-disposal write access.</param>
    /// <remarks>
    /// Skipped (inconclusive) when <typeparamref name="TAlgorithm" /> does not implement <see cref="IDisposable" />.
    /// Reads the property value before disposal, then reassigns the same value after disposal — exercising the
    /// setter without changing observable behaviour.
    /// </remarks>
    [TestMethod]
    [DynamicData(
        nameof(GetAlgorithmWritableProperties),
        DynamicDataDisplayName = nameof(TestHelpers.GetTypePropertyDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TestHelpers))]
    public void Dispose_WhenAssigningProperty_ShouldThrowExactly(PropertyInfo property)
    {
        if (!typeof(TAlgorithm).IsAssignableTo(typeof(IDisposable)))
        {
            Assert.Inconclusive(
                $"Type '{typeof(TAlgorithm).Name}' does not implement IDisposable; disposal property-write test skipped.");
            return;
        }

        if (property is null)
        {
            Assert.Inconclusive(
                $"Type '{typeof(TAlgorithm).Name}' has no writable properties - test passes by default.");
            return;
        }

        TAlgorithm algorithm = CreateAlgorithm();

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

        ((IDisposable)algorithm).Dispose();

        try
        {
            property.SetValue(algorithm, currentValue);
            Assert.Fail(
                $"Expected ObjectDisposedException when setting property '{property.Name}' after disposal.");
        }
        catch (TargetInvocationException tie) when (tie.InnerException is ObjectDisposedException ex)
        {
            Assert.AreEqual(
                typeof(TAlgorithm).FullName,
                ex.ObjectName,
                $"ObjectDisposedException.ObjectName must match the concrete type name '{typeof(TAlgorithm).FullName}'.");
        }
        catch (Exception ex)
        {
            Assert.Fail(
                $"Unexpected exception when setting property '{property.Name}' after disposal: {ex.GetType().Name} - {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that every writable instance field on the algorithm is cleared (null or default) after
    /// <see cref="IDisposable.Dispose" /> has been called on an exercised instance.
    /// </summary>
    /// <param name="field">The field to validate after disposal.</param>
    /// <remarks>
    /// Skipped (inconclusive) when <typeparamref name="TAlgorithm" /> does not implement <see cref="IDisposable" />,
    /// or when the algorithm declares no writable instance fields that participate in the disposal contract.
    /// </remarks>
    [TestMethod]
    [DynamicData(
        nameof(GetAlgorithmFields),
        DynamicDataDisplayName = nameof(TestHelpers.GetTypeFieldDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TestHelpers))]
    public void Dispose_WhenCalled_ShouldZeroPrivateField(FieldInfo field)
    {
        if (!typeof(TAlgorithm).IsAssignableTo(typeof(IDisposable)))
        {
            Assert.Inconclusive(
                $"Type '{typeof(TAlgorithm).Name}' does not implement IDisposable; disposal field-zero test skipped.");
            return;
        }

        if (field is null)
        {
            Assert.Inconclusive(
                $"Type '{typeof(TAlgorithm).Name}' has no writable instance fields - test passes by default.");
            return;
        }

        TAlgorithm algorithm = CreateAlgorithm();

        // Exercise the instance so any lazily-populated state is realised before disposal.
        algorithm.Append([0x00, 0x01, 0x02, 0x03]);
        _ = algorithm.GetCurrentHash();

        ((IDisposable)algorithm).Dispose();

        var result = TestHelpers.AssertFieldValueIsNullOrDefault(field, algorithm);

        Assert.IsTrue(
            result,
            $"Field '{field.DeclaringType},{field.Name}' value '{field.GetValue(algorithm)}' is not null or default after disposal.");
    }

    /// <summary>
    /// Verifies that calling <see cref="NonCryptographicHashAlgorithm.GetCurrentHash()" /> after disposal throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    /// <remarks>
    /// Skipped (inconclusive) when <typeparamref name="TAlgorithm" /> does not implement <see cref="IDisposable" />.
    /// </remarks>
    [TestMethod]
    public void Dispose_WhenGetCurrentHashCalledAfterDispose_ShouldThrowExactly()
    {
        if (!typeof(TAlgorithm).IsAssignableTo(typeof(IDisposable)))
        {
            Assert.Inconclusive(
                $"Type '{typeof(TAlgorithm).Name}' does not implement IDisposable; disposal GetCurrentHash test skipped.");
            return;
        }

        TAlgorithm algorithm = CreateAlgorithm();
        ((IDisposable)algorithm).Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = algorithm.GetCurrentHash();
        });
    }
    /// <summary>
    /// Verifies that disposing a freshly-constructed instance of the algorithm under test — one that has never had
    /// any property accessed or hashing performed — completes without throwing.
    /// </summary>
    /// <remarks>
    /// Skipped (inconclusive) when <typeparamref name="TAlgorithm" /> does not implement <see cref="IDisposable" />,
    /// because the disposal contract is irrelevant for non-disposable types.
    /// </remarks>
    [TestMethod]
    public void Dispose_WhenInstanceUntouched_ShouldNotThrow()
    {
        if (!typeof(TAlgorithm).IsAssignableTo(typeof(IDisposable)))
        {
            Assert.Inconclusive(
                $"Type '{typeof(TAlgorithm).Name}' does not implement IDisposable; disposal contract test skipped.");
            return;
        }

        TAlgorithm algorithm = CreateAlgorithm();

        try
        {
            ((IDisposable)algorithm).Dispose();
        }
        catch (Exception ex)
        {
            Assert.Fail(
                $"Disposing an untouched {typeof(TAlgorithm).Name} instance threw {ex.GetType().Name}: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifies that readable public properties on the algorithm throw <see cref="ObjectDisposedException" /> when
    /// accessed after the instance has been disposed.
    /// </summary>
    /// <param name="property">The property to test for post-disposal read access.</param>
    /// <remarks>
    /// Skipped (inconclusive) when <typeparamref name="TAlgorithm" /> does not implement <see cref="IDisposable" />.
    /// Concrete test classes may override <see cref="ExcludedReadablePropertyNames" /> to skip individual
    /// properties that are intentionally accessible after disposal.
    /// </remarks>
    [TestMethod]
    [DynamicData(
        nameof(GetAlgorithmReadableProperties),
        DynamicDataDisplayName = nameof(TestHelpers.GetTypePropertyDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(TestHelpers))]
    public void Dispose_WhenReadingProperty_ShouldThrowExactly(PropertyInfo property)
    {
        if (!typeof(TAlgorithm).IsAssignableTo(typeof(IDisposable)))
        {
            Assert.Inconclusive(
                $"Type '{typeof(TAlgorithm).Name}' does not implement IDisposable; disposal property-read test skipped.");
            return;
        }

        if (property is null)
        {
            Assert.Inconclusive(
                $"Type '{typeof(TAlgorithm).Name}' has no readable properties - test passes by default.");
            return;
        }

        TAlgorithm algorithm = CreateAlgorithm();
        ((IDisposable)algorithm).Dispose();

        try
        {
            _ = property.GetValue(algorithm);
            Assert.Fail(
                $"Expected ObjectDisposedException when reading property '{property.Name}' after disposal.");
        }
        catch (TargetInvocationException tie) when (tie.InnerException is ObjectDisposedException ex)
        {
            Assert.AreEqual(
                typeof(TAlgorithm).FullName,
                ex.ObjectName,
                $"ObjectDisposedException.ObjectName must match the concrete type name '{typeof(TAlgorithm).FullName}'.");
        }
        catch (Exception ex)
        {
            Assert.Fail(
                $"Unexpected exception when reading property '{property.Name}' after disposal: {ex.GetType().Name} - {ex.Message}");
        }
    }

}
