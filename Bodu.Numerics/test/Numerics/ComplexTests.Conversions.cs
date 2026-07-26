// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComplexTests.Conversions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Numerics;

public partial class ComplexTests
{
    /// <summary>
    /// Verifies that the implicit conversion from a real value places it on the real axis.
    /// </summary>
    [TestMethod]
    public void ImplicitConversion_FromReal_ShouldPlaceValueOnRealAxis()
    {
        Complex<double> value = 5.0;

        Assert.AreEqual(new Complex<double>(5.0, 0.0), value);
    }

    /// <summary>
    /// Verifies that the generic-math checked conversion from an integer embeds it as the real component.
    /// </summary>
    [TestMethod]
    public void CreateChecked_FromInteger_ShouldEmbedAsRealComponent()
    {
        Complex<double> value = CreateChecked<Complex<double>, int>(7);

        Assert.AreEqual(new Complex<double>(7.0, 0.0), value);
    }

    /// <summary>
    /// Verifies that the generic-math checked conversion of a real-valued complex to a scalar returns the real part.
    /// </summary>
    [TestMethod]
    public void CreateChecked_ToScalar_WhenValueIsReal_ShouldReturnRealComponent()
    {
        double value = double.CreateChecked(new Complex<double>(4.0, 0.0));

        Assert.AreEqual(4.0, value);
    }

    /// <summary>
    /// Verifies that the generic-math checked conversion of a non-real complex to a scalar throws
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void CreateChecked_ToScalar_WhenValueHasImaginaryPart_ShouldThrowNotSupported()
    {
        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = double.CreateChecked(new Complex<double>(4.0, 1.0));
        });
    }

    /// <summary>Invokes the generic-math checked conversion between two number types.</summary>
    /// <typeparam name="TResult">The destination number type.</typeparam>
    /// <typeparam name="TSource">The source number type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns>The converted value.</returns>
    private static TResult CreateChecked<TResult, TSource>(TSource value)
        where TResult : INumberBase<TResult>
        where TSource : INumberBase<TSource> =>
        TResult.CreateChecked(value);
}
