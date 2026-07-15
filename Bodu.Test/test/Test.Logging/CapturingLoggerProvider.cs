// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CapturingLoggerProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Test.Logging;

/// <summary>
/// An <see cref="ILoggerProvider" /> that hands out a single shared <see cref="CapturingLogger" />, so a test can wire
/// a capturing logger into a service collection and assert on what a resolved component logged.
/// </summary>
public sealed class CapturingLoggerProvider
    : ILoggerProvider
{
    /// <summary>
    /// Gets the shared logger every category resolves to.
    /// </summary>
    /// <value>The capturing logger.</value>
    public CapturingLogger Logger { get; } = new();

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => Logger;

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
