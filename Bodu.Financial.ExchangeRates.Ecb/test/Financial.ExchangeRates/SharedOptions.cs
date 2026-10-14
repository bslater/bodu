// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SharedOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Reads a provider's options through the <see cref="WebRateProviderOptions" /> surface they inherit, so a test sets
/// and reads the shared members as the base type declares them.
/// </summary>
internal static class SharedOptions
{
    /// <summary>
    /// Returns the options typed as <see cref="WebRateProviderOptions" />, failing the test when the options type does
    /// not derive from it.
    /// </summary>
    /// <param name="options">The options instance.</param>
    /// <returns>The same instance, typed as <see cref="WebRateProviderOptions" />.</returns>
    public static WebRateProviderOptions Of(object options)
    {
        Assert.IsInstanceOfType<WebRateProviderOptions>(options);

        return (WebRateProviderOptions)options;
    }
}
